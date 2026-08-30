using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Platform.Commands.ChangeOrganizationSubscription;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Platform.OrganizationSubscriptions;
using MyCondo.Domain.Features.Platform.OrganizationSubscriptions.Exceptions;
using MyCondo.Domain.Features.Platform.SubscriptionPackages;
using MyCondo.Domain.Features.Tenancy;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Platform.Commands.ChangeOrganizationSubscription;

public class ChangeOrganizationSubscriptionCommandHandlerTests
{
    private static readonly DateTimeOffset NowUtc = new(2026, 8, 10, 0, 0, 0, TimeSpan.Zero);

    private readonly ITenantRepository _tenants = Substitute.For<ITenantRepository>();
    private readonly IOrganizationSubscriptionRepository _organizationSubscriptions = Substitute.For<IOrganizationSubscriptionRepository>();
    private readonly ISubscriptionPackageRepository _subscriptionPackages = Substitute.For<ISubscriptionPackageRepository>();
    private readonly ISubscriptionPackageVersionRepository _subscriptionPackageVersions = Substitute.For<ISubscriptionPackageVersionRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IClock _clock = Substitute.For<IClock>();

    private readonly Tenant _tenant = Tenant.Provision("Akter Residence Park", "arp", NowUtc);
    private readonly SubscriptionPackage _activePackage;
    private readonly SubscriptionPackageVersion _activePackageVersion;
    private readonly OrganizationSubscription _currentSubscription;

    public ChangeOrganizationSubscriptionCommandHandlerTests()
    {
        _activePackage = SubscriptionPackage.Create("PRO", "Professional", description: null);
        _activePackageVersion = SubscriptionPackageVersion.Create(
            _activePackage.Id,
            version: 2,
            effectiveFrom: DateOnly.FromDateTime(NowUtc.UtcDateTime).AddDays(-1),
            effectiveUntil: null,
            monthlyPrice: 1200m,
            quarterlyPrice: null,
            semiAnnualPrice: null,
            annualPrice: 12000m,
            currency: "BDT");
        _activePackageVersion.Activate();
        _activePackage.Activate();
        _activePackage.SetCurrentVersion(_activePackageVersion.Id);

        _currentSubscription = OrganizationSubscription.Create(
            _tenant.Id.Value,
            SubscriptionPackageVersionId.New(),
            BillingCycle.Monthly,
            startDate: DateOnly.FromDateTime(NowUtc.UtcDateTime).AddMonths(-1),
            endDate: null,
            nextBillingDate: null,
            basePrice: 800m,
            discount: 100m,
            currency: "BDT",
            activatedAtUtc: NowUtc.AddMonths(-1),
            autoRenew: false);

        _tenants.GetByIdAsync(_tenant.Id.Value, Arg.Any<CancellationToken>()).Returns(_tenant);
        _organizationSubscriptions.GetCurrentForTenantAsync(_tenant.Id.Value, Arg.Any<CancellationToken>())
            .Returns(_currentSubscription);
        _subscriptionPackages.GetAllAsync(Arg.Any<CancellationToken>()).Returns([_activePackage]);
        _subscriptionPackageVersions.GetAllAsync(Arg.Any<CancellationToken>()).Returns([_activePackageVersion]);
        _clock.UtcNow.Returns(NowUtc);
    }

    private ChangeOrganizationSubscriptionCommandHandler CreateHandler() => new(
        _tenants, _organizationSubscriptions, _subscriptionPackages, _subscriptionPackageVersions,
        _unitOfWork, _clock, Substitute.For<ILogger<ChangeOrganizationSubscriptionCommandHandler>>());

    private ChangeOrganizationSubscriptionCommand ValidCommand() => new(
        OrganizationId: _tenant.Id.Value,
        SubscriptionPackageVersionId: _activePackageVersion.Id.Value,
        BillingCycle: BillingCycle.Annual,
        AutoRenew: true);

    [Fact]
    public async Task Assigns_The_Exact_Caller_Selected_Package_Version_Cycle_And_AutoRenew()
    {
        await CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        _currentSubscription.PackageVersionId.Should().Be(_activePackageVersion.Id);
        _currentSubscription.BillingCycle.Should().Be(BillingCycle.Annual);
        _currentSubscription.AutoRenew.Should().BeTrue();
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Resolves_A_Fresh_Commercial_Snapshot_From_The_New_Version_And_Cycle()
    {
        await CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        _currentSubscription.BasePrice.Should().Be(12000m);
        _currentSubscription.Discount.Should().Be(0m);
        _currentSubscription.EffectivePrice.Should().Be(12000m);
        _currentSubscription.Currency.Should().Be("BDT");
    }

    [Fact]
    public async Task Throws_NotFound_When_Organization_Does_Not_Exist()
    {
        Guid organizationId = Guid.NewGuid();
        _tenants.GetByIdAsync(organizationId, Arg.Any<CancellationToken>()).Returns((Tenant?)null);

        Func<Task> act = async () => await CreateHandler().Handle(
            ValidCommand() with { OrganizationId = organizationId }, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Throws_NotFound_When_Organization_Has_No_Current_Subscription()
    {
        _organizationSubscriptions.GetCurrentForTenantAsync(_tenant.Id.Value, Arg.Any<CancellationToken>())
            .Returns((OrganizationSubscription?)null);

        Func<Task> act = async () => await CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Throws_NotFound_When_SubscriptionPackageVersion_Does_Not_Exist()
    {
        ChangeOrganizationSubscriptionCommand command = ValidCommand() with { SubscriptionPackageVersionId = Guid.NewGuid() };

        Func<Task> act = async () => await CreateHandler().Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Throws_Conflict_When_SubscriptionPackageVersion_Is_Not_Active()
    {
        SubscriptionPackage draftPackage = SubscriptionPackage.Create("DRAFT", "Draft Package", description: null);
        SubscriptionPackageVersion draftVersion = SubscriptionPackageVersion.Create(
            draftPackage.Id, version: 1, effectiveFrom: DateOnly.FromDateTime(NowUtc.UtcDateTime), effectiveUntil: null,
            monthlyPrice: 500m, quarterlyPrice: null, semiAnnualPrice: null, annualPrice: null, currency: "BDT");
        _subscriptionPackages.GetAllAsync(Arg.Any<CancellationToken>()).Returns([draftPackage]);
        _subscriptionPackageVersions.GetAllAsync(Arg.Any<CancellationToken>()).Returns([draftVersion]);

        ChangeOrganizationSubscriptionCommand command = ValidCommand() with { SubscriptionPackageVersionId = draftVersion.Id.Value };

        Func<Task> act = async () => await CreateHandler().Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Throws_Conflict_When_PackageVersion_Is_Not_Yet_Effective()
    {
        SubscriptionPackage futurePackage = SubscriptionPackage.Create("FUT", "Future Package", description: null);
        SubscriptionPackageVersion futureVersion = SubscriptionPackageVersion.Create(
            futurePackage.Id, version: 1, effectiveFrom: DateOnly.FromDateTime(NowUtc.UtcDateTime).AddDays(30),
            effectiveUntil: null, monthlyPrice: 500m, quarterlyPrice: null, semiAnnualPrice: null, annualPrice: null,
            currency: "BDT");
        futureVersion.Activate();
        futurePackage.Activate();
        futurePackage.SetCurrentVersion(futureVersion.Id);
        _subscriptionPackages.GetAllAsync(Arg.Any<CancellationToken>()).Returns([futurePackage]);
        _subscriptionPackageVersions.GetAllAsync(Arg.Any<CancellationToken>()).Returns([futureVersion]);

        ChangeOrganizationSubscriptionCommand command = ValidCommand() with { SubscriptionPackageVersionId = futureVersion.Id.Value };

        Func<Task> act = async () => await CreateHandler().Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Throws_Conflict_When_The_Legacy_Grandfathered_Package_Is_Selected()
    {
        SubscriptionPackage legacyPackage = SubscriptionPackage.Create(
            "LEGACY-MIGRATION-GRANDFATHERED", "Legacy Migration (Grandfathered)", description: null);
        SubscriptionPackageVersion legacyVersion = SubscriptionPackageVersion.Create(
            legacyPackage.Id, version: 1, effectiveFrom: DateOnly.FromDateTime(NowUtc.UtcDateTime), effectiveUntil: null,
            monthlyPrice: 0m, quarterlyPrice: null, semiAnnualPrice: null, annualPrice: null, currency: "BDT");
        legacyVersion.Activate();
        legacyPackage.Activate();
        legacyPackage.SetCurrentVersion(legacyVersion.Id);
        _subscriptionPackages.GetAllAsync(Arg.Any<CancellationToken>()).Returns([legacyPackage]);
        _subscriptionPackageVersions.GetAllAsync(Arg.Any<CancellationToken>()).Returns([legacyVersion]);

        ChangeOrganizationSubscriptionCommand command = ValidCommand() with { SubscriptionPackageVersionId = legacyVersion.Id.Value };

        Func<Task> act = async () => await CreateHandler().Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Throws_UnsupportedBillingCycle_When_The_New_Version_Does_Not_Price_The_Requested_Cycle()
    {
        // _activePackageVersion configures Monthly and Annual only.
        ChangeOrganizationSubscriptionCommand command = ValidCommand() with { BillingCycle = BillingCycle.Quarterly };

        Func<Task> act = async () => await CreateHandler().Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<UnsupportedBillingCycleException>();
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Throws_When_Current_Subscription_Is_Not_Active_Or_PastDue()
    {
        _currentSubscription.Cancel(NowUtc);

        Func<Task> act = async () => await CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<OrganizationSubscriptionChangeNotAllowedException>();
    }
}
