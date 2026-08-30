using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Platform.Commands.RecordSubscriptionPayment;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Platform.OrganizationSubscriptions;
using MyCondo.Domain.Features.Platform.SubscriptionInvoices;
using MyCondo.Domain.Features.Platform.SubscriptionInvoices.Exceptions;
using MyCondo.Domain.Features.Platform.SubscriptionPackages;
using MyCondo.Domain.Features.Platform.SubscriptionPayments;
using MyCondo.Domain.Features.Tenancy;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Platform.Commands.RecordSubscriptionPayment;

public class RecordSubscriptionPaymentCommandHandlerTests
{
    private static readonly DateTimeOffset NowUtc = new(2026, 3, 5, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly PaymentDate = new(2026, 3, 5);

    private readonly ITenantRepository _tenants = Substitute.For<ITenantRepository>();
    private readonly ISubscriptionInvoiceRepository _subscriptionInvoices = Substitute.For<ISubscriptionInvoiceRepository>();
    private readonly ISubscriptionPaymentRepository _subscriptionPayments = Substitute.For<ISubscriptionPaymentRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IClock _clock = Substitute.For<IClock>();

    private readonly Tenant _tenant = Tenant.Provision("Akter Residence Park", "arp", NowUtc);
    private readonly SubscriptionInvoice _invoice;

    public RecordSubscriptionPaymentCommandHandlerTests()
    {
        (_invoice, _) = SubscriptionInvoice.Issue(
            _tenant.Id.Value, OrganizationSubscriptionId.New(), "SUB-INV-2026-03",
            new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 31), new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 31),
            "BDT",
            [new SubscriptionInvoiceLineInput(
                SubscriptionPackageVersionId.New(), "Professional", 1,
                BillingCycle.Monthly, 8000m, 0m, 8000m, "Professional (Monthly)")],
            NowUtc);

        _tenants.GetByIdAsync(_tenant.Id.Value, Arg.Any<CancellationToken>()).Returns(_tenant);
        _subscriptionInvoices.GetByIdAsync(_invoice.Id, Arg.Any<CancellationToken>()).Returns(_invoice);
        _subscriptionPayments.ExistsByReferenceAsync(_tenant.Id.Value, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);
        _clock.UtcNow.Returns(NowUtc);
    }

    private RecordSubscriptionPaymentCommandHandler CreateHandler() => new(
        _tenants, _subscriptionInvoices, _subscriptionPayments, _unitOfWork, _clock,
        Substitute.For<ILogger<RecordSubscriptionPaymentCommandHandler>>());

    private RecordSubscriptionPaymentCommand ValidCommand(decimal amount = 3000m, string referenceNumber = "BANK-REF-001") =>
        new(_tenant.Id.Value, _invoice.Id.Value, amount, "BDT", PaymentDate, referenceNumber, "Wire transfer");

    [Fact]
    public async Task Records_A_Payment_Referencing_The_Correct_Tenant_And_Invoice()
    {
        Guid paymentId = await CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        paymentId.Should().NotBe(Guid.Empty);
        _subscriptionPayments.Received(1).Add(Arg.Is<SubscriptionPayment>(p =>
            p.TenantId == _tenant.Id.Value && p.SubscriptionInvoiceId == _invoice.Id && p.Amount == 3000m));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Partial_Payment_Reduces_Outstanding_And_Leaves_Invoice_Issued()
    {
        await CreateHandler().Handle(ValidCommand(amount: 3000m), CancellationToken.None);

        _invoice.OutstandingAmount.Should().Be(5000m);
        _invoice.Status.Should().Be(SubscriptionInvoiceStatus.Issued);
    }

    [Fact]
    public async Task Full_Payment_Zeroes_Outstanding_And_Transitions_Invoice_To_Paid()
    {
        await CreateHandler().Handle(ValidCommand(amount: 8000m), CancellationToken.None);

        _invoice.OutstandingAmount.Should().Be(0m);
        _invoice.Status.Should().Be(SubscriptionInvoiceStatus.Paid);
    }

    [Fact]
    public async Task Throws_NotFound_When_Organization_Does_Not_Exist()
    {
        Guid organizationId = Guid.NewGuid();
        _tenants.GetByIdAsync(organizationId, Arg.Any<CancellationToken>()).Returns((Tenant?)null);

        Func<Task> act = async () => await CreateHandler().Handle(
            ValidCommand() with { OrganizationId = organizationId }, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        _subscriptionPayments.DidNotReceive().Add(Arg.Any<SubscriptionPayment>());
    }

    [Fact]
    public async Task Throws_NotFound_When_Invoice_Does_Not_Exist()
    {
        Guid invoiceId = Guid.NewGuid();
        _subscriptionInvoices.GetByIdAsync(new SubscriptionInvoiceId(invoiceId), Arg.Any<CancellationToken>())
            .Returns((SubscriptionInvoice?)null);

        Func<Task> act = async () => await CreateHandler().Handle(
            ValidCommand() with { SubscriptionInvoiceId = invoiceId }, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        _subscriptionPayments.DidNotReceive().Add(Arg.Any<SubscriptionPayment>());
    }

    [Fact]
    public async Task Throws_NotFound_When_Invoice_Belongs_To_A_Different_Organization()
    {
        Tenant otherTenant = Tenant.Provision("Other Organization", "other", NowUtc);
        _tenants.GetByIdAsync(otherTenant.Id.Value, Arg.Any<CancellationToken>()).Returns(otherTenant);

        Func<Task> act = async () => await CreateHandler().Handle(
            ValidCommand() with { OrganizationId = otherTenant.Id.Value }, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        _subscriptionPayments.DidNotReceive().Add(Arg.Any<SubscriptionPayment>());
    }

    [Fact]
    public async Task Throws_Conflict_When_Reference_Number_Was_Already_Used()
    {
        _subscriptionPayments.ExistsByReferenceAsync(_tenant.Id.Value, "DUP-REF", Arg.Any<CancellationToken>())
            .Returns(true);

        Func<Task> act = async () => await CreateHandler().Handle(
            ValidCommand(referenceNumber: "DUP-REF"), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
        _subscriptionPayments.DidNotReceive().Add(Arg.Any<SubscriptionPayment>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Throws_When_Currency_Does_Not_Match_The_Invoice()
    {
        Func<Task> act = async () => await CreateHandler().Handle(
            ValidCommand() with { Currency = "USD" }, CancellationToken.None);

        await act.Should().ThrowAsync<SubscriptionInvoiceCurrencyMismatchException>();
        _subscriptionPayments.DidNotReceive().Add(Arg.Any<SubscriptionPayment>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Throws_When_Amount_Exceeds_Outstanding()
    {
        Func<Task> act = async () => await CreateHandler().Handle(
            ValidCommand(amount: 8000.01m), CancellationToken.None);

        await act.Should().ThrowAsync<SubscriptionInvoicePaymentExceedsOutstandingException>();
        _subscriptionPayments.DidNotReceive().Add(Arg.Any<SubscriptionPayment>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Throws_When_Invoice_Is_Already_Paid()
    {
        _invoice.ApplyPayment(8000m, "BDT", NowUtc);

        Func<Task> act = async () => await CreateHandler().Handle(ValidCommand(amount: 1000m), CancellationToken.None);

        await act.Should().ThrowAsync<SubscriptionInvoiceNotPayableException>();
        _subscriptionPayments.DidNotReceive().Add(Arg.Any<SubscriptionPayment>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Throws_When_Invoice_Is_Void()
    {
        _invoice.Void("wrong subscription", NowUtc);

        Func<Task> act = async () => await CreateHandler().Handle(ValidCommand(amount: 1000m), CancellationToken.None);

        await act.Should().ThrowAsync<SubscriptionInvoiceNotPayableException>();
        _subscriptionPayments.DidNotReceive().Add(Arg.Any<SubscriptionPayment>());
    }
}
