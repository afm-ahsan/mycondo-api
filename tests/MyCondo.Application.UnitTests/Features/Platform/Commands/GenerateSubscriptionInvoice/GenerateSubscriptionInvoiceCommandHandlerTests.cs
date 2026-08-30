using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Platform.Commands.GenerateSubscriptionInvoice;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Platform.OrganizationSubscriptions;
using MyCondo.Domain.Features.Platform.SubscriptionInvoices;
using MyCondo.Domain.Features.Platform.SubscriptionPackages;
using MyCondo.Domain.Features.Tenancy;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Platform.Commands.GenerateSubscriptionInvoice;

public class GenerateSubscriptionInvoiceCommandHandlerTests
{
    private static readonly DateTimeOffset NowUtc = new(2026, 4, 5, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly PeriodStart = new(2026, 3, 1);

    private readonly ITenantRepository _tenants = Substitute.For<ITenantRepository>();
    private readonly IOrganizationSubscriptionRepository _organizationSubscriptions = Substitute.For<IOrganizationSubscriptionRepository>();
    private readonly ISubscriptionInvoiceRepository _subscriptionInvoices = Substitute.For<ISubscriptionInvoiceRepository>();
    private readonly ISubscriptionPackageRepository _subscriptionPackages = Substitute.For<ISubscriptionPackageRepository>();
    private readonly ISubscriptionPackageVersionRepository _subscriptionPackageVersions = Substitute.For<ISubscriptionPackageVersionRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IClock _clock = Substitute.For<IClock>();

    private readonly Tenant _tenant = Tenant.Provision("Akter Residence Park", "arp", NowUtc);
    private readonly SubscriptionPackage _package;
    private readonly SubscriptionPackageVersion _packageVersion;
    private readonly OrganizationSubscription _subscription;

    public GenerateSubscriptionInvoiceCommandHandlerTests()
    {
        _package = SubscriptionPackage.Create("PRO", "Professional", description: null);
        _packageVersion = SubscriptionPackageVersion.Create(
            _package.Id, version: 3, effectiveFrom: PeriodStart.AddYears(-1), effectiveUntil: null,
            // Deliberately different from the subscription's own snapshot below, so tests can prove the
            // invoice is priced from the subscription snapshot, never re-derived from this live version.
            monthlyPrice: 9999m, quarterlyPrice: null, semiAnnualPrice: null, annualPrice: null, currency: "BDT");
        _packageVersion.Activate();
        _package.Activate();
        _package.SetCurrentVersion(_packageVersion.Id);

        _subscription = OrganizationSubscription.Create(
            _tenant.Id.Value, _packageVersion.Id, BillingCycle.Monthly,
            startDate: PeriodStart.AddMonths(-1), endDate: null, nextBillingDate: null,
            basePrice: 1000m, discount: 100m, currency: "BDT", activatedAtUtc: NowUtc.AddMonths(-1), autoRenew: false);

        _tenants.GetByIdAsync(_tenant.Id.Value, Arg.Any<CancellationToken>()).Returns(_tenant);
        _organizationSubscriptions.GetCurrentForTenantAsync(_tenant.Id.Value, Arg.Any<CancellationToken>())
            .Returns(_subscription);
        _subscriptionInvoices.GetForOrganizationSubscriptionAsync(_subscription.Id, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<SubscriptionInvoice>)[]);
        _subscriptionPackages.GetAllAsync(Arg.Any<CancellationToken>()).Returns([_package]);
        _subscriptionPackageVersions.GetAllAsync(Arg.Any<CancellationToken>()).Returns([_packageVersion]);
        _clock.UtcNow.Returns(NowUtc);
    }

    private GenerateSubscriptionInvoiceCommandHandler CreateHandler() => new(
        _tenants, _organizationSubscriptions, _subscriptionInvoices, _subscriptionPackages,
        _subscriptionPackageVersions, _unitOfWork, _clock,
        Substitute.For<ILogger<GenerateSubscriptionInvoiceCommandHandler>>());

    private GenerateSubscriptionInvoiceCommand ValidCommand() => new(_tenant.Id.Value, PeriodStart);

    [Fact]
    public async Task Generates_Exactly_One_Invoice_Referencing_The_Correct_Tenant_And_Subscription()
    {
        Guid invoiceId = await CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        invoiceId.Should().NotBe(Guid.Empty);
        _subscriptionInvoices.Received(1).Add(Arg.Is<SubscriptionInvoice>(i =>
            i.TenantId == _tenant.Id.Value && i.OrganizationSubscriptionId == _subscription.Id));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Snapshots_The_Exact_Subscription_Package_Version_On_The_Line()
    {
        SubscriptionInvoiceLine? capturedLine = null;
        _subscriptionInvoices.When(x => x.AddLines(Arg.Any<IEnumerable<SubscriptionInvoiceLine>>()))
            .Do(call => capturedLine = call.Arg<IEnumerable<SubscriptionInvoiceLine>>().Single());

        await CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        capturedLine.Should().NotBeNull();
        capturedLine!.PackageVersionId.Should().Be(_packageVersion.Id);
        capturedLine.PackageVersionNumberSnapshot.Should().Be(3);
        capturedLine.PackageNameSnapshot.Should().Be("Professional");
    }

    [Fact]
    public async Task Prices_The_Invoice_From_The_Subscription_Snapshot_Not_The_Live_Package_Version()
    {
        SubscriptionInvoice? capturedInvoice = null;
        _subscriptionInvoices.When(x => x.Add(Arg.Any<SubscriptionInvoice>()))
            .Do(call => capturedInvoice = call.Arg<SubscriptionInvoice>());

        await CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        // Subscription snapshot is BasePrice 1000 / Discount 100 / EffectivePrice 900 — the package
        // version's own MonthlyPrice (9999) must never leak into the charge.
        capturedInvoice.Should().NotBeNull();
        capturedInvoice!.TotalAmount.Should().Be(900m);
        capturedInvoice.OutstandingAmount.Should().Be(900m);
        capturedInvoice.Currency.Should().Be("BDT");
        capturedInvoice.Status.Should().Be(SubscriptionInvoiceStatus.Issued);
    }

    [Fact]
    public async Task Derives_The_Billing_Period_End_From_The_Subscription_BillingCycle()
    {
        SubscriptionInvoice? capturedInvoice = null;
        _subscriptionInvoices.When(x => x.Add(Arg.Any<SubscriptionInvoice>()))
            .Do(call => capturedInvoice = call.Arg<SubscriptionInvoice>());

        await CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        capturedInvoice!.BillingPeriodStart.Should().Be(PeriodStart);
        capturedInvoice.BillingPeriodEnd.Should().Be(new DateOnly(2026, 3, 31));
    }

    [Fact]
    public async Task DueDate_Is_Clamped_To_IssueDate_When_Generated_Retroactively_After_Period_End()
    {
        // NowUtc (2026-04-05) is after the March billing period's own end (2026-03-31) — the exact
        // "admin invoices a period after it already ended" case DueDate=PeriodEnd alone cannot satisfy
        // (SubscriptionInvoice.Issue rejects DueDate < IssueDate).
        SubscriptionInvoice? capturedInvoice = null;
        _subscriptionInvoices.When(x => x.Add(Arg.Any<SubscriptionInvoice>()))
            .Do(call => capturedInvoice = call.Arg<SubscriptionInvoice>());

        await CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        capturedInvoice!.IssueDate.Should().Be(DateOnly.FromDateTime(NowUtc.UtcDateTime));
        capturedInvoice.DueDate.Should().Be(capturedInvoice.IssueDate);
    }

    [Fact]
    public async Task DueDate_Is_The_Billing_Period_End_When_Generated_Before_Period_End()
    {
        _clock.UtcNow.Returns(new DateTimeOffset(2026, 3, 5, 10, 0, 0, TimeSpan.Zero));

        SubscriptionInvoice? capturedInvoice = null;
        _subscriptionInvoices.When(x => x.Add(Arg.Any<SubscriptionInvoice>()))
            .Do(call => capturedInvoice = call.Arg<SubscriptionInvoice>());

        await CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        capturedInvoice!.BillingPeriodEnd.Should().Be(new DateOnly(2026, 3, 31));
        capturedInvoice.DueDate.Should().Be(capturedInvoice.BillingPeriodEnd);
    }

    [Fact]
    public async Task Throws_NotFound_When_Organization_Does_Not_Exist()
    {
        Guid organizationId = Guid.NewGuid();
        _tenants.GetByIdAsync(organizationId, Arg.Any<CancellationToken>()).Returns((Tenant?)null);

        Func<Task> act = async () => await CreateHandler().Handle(
            ValidCommand() with { OrganizationId = organizationId }, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        _subscriptionInvoices.DidNotReceive().Add(Arg.Any<SubscriptionInvoice>());
    }

    [Fact]
    public async Task Throws_NotFound_When_Organization_Has_No_Current_Subscription()
    {
        _organizationSubscriptions.GetCurrentForTenantAsync(_tenant.Id.Value, Arg.Any<CancellationToken>())
            .Returns((OrganizationSubscription?)null);

        Func<Task> act = async () => await CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        _subscriptionInvoices.DidNotReceive().Add(Arg.Any<SubscriptionInvoice>());
    }

    [Fact]
    public async Task Throws_Conflict_When_Subscription_Is_Restricted()
    {
        _subscription.MarkPastDue();
        _subscription.Restrict(NowUtc);

        Func<Task> act = async () => await CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
        _subscriptionInvoices.DidNotReceive().Add(Arg.Any<SubscriptionInvoice>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Throws_Conflict_When_An_Invoice_Already_Exists_For_The_Same_Period()
    {
        (SubscriptionInvoice existing, _) = SubscriptionInvoice.Issue(
            _tenant.Id.Value, _subscription.Id, "SUB-INV-EXISTING", PeriodStart, new DateOnly(2026, 3, 31),
            PeriodStart, new DateOnly(2026, 3, 31), "BDT",
            [new SubscriptionInvoiceLineInput(_packageVersion.Id, "Professional", 3, BillingCycle.Monthly, 1000m, 100m, 900m, "x")],
            NowUtc);
        _subscriptionInvoices.GetForOrganizationSubscriptionAsync(_subscription.Id, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<SubscriptionInvoice>)[existing]);

        Func<Task> act = async () => await CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
        _subscriptionInvoices.DidNotReceive().Add(Arg.Any<SubscriptionInvoice>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Allows_A_Different_Period_For_The_Same_Subscription_After_An_Existing_Invoice()
    {
        (SubscriptionInvoice existing, _) = SubscriptionInvoice.Issue(
            _tenant.Id.Value, _subscription.Id, "SUB-INV-EARLIER", PeriodStart.AddMonths(-1), PeriodStart.AddDays(-1),
            PeriodStart.AddMonths(-1), PeriodStart.AddDays(-1), "BDT",
            [new SubscriptionInvoiceLineInput(_packageVersion.Id, "Professional", 3, BillingCycle.Monthly, 1000m, 100m, 900m, "x")],
            NowUtc);
        _subscriptionInvoices.GetForOrganizationSubscriptionAsync(_subscription.Id, Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<SubscriptionInvoice>)[existing]);

        Guid invoiceId = await CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        invoiceId.Should().NotBe(Guid.Empty);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
