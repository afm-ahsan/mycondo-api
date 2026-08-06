using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Payments.Commands.RecordPayment;
using MyCondo.Application.Features.Payments.DTOs;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Billing.Invoices;
using MyCondo.Domain.Features.Billing.ServiceChargeRules;
using MyCondo.Domain.Features.Payments.Ledger;
using MyCondo.Domain.Features.Payments.PaymentAllocations;
using MyCondo.Domain.Features.Payments.Payments;
using MyCondo.Domain.Features.Payments.ResidentAccounts;
using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Domain.Features.Property.Flats;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Payments.Commands.RecordPayment;

/// <summary>
/// Handler-level tests for RecordPaymentCommandHandler's FIFO allocation math (min(remaining,
/// balance) per invoice, in whatever order the repository returns them). The repository's own
/// deterministic ORDER BY (due_date, invoice_date, invoice_number) — the actual "FIFO" ordering
/// guarantee — requires a real Postgres connection to prove and is covered by
/// PaymentsCrossTenantIsolationTests-style MultiTenancyTests, which need Docker (not runnable in
/// this environment, same disclosed limitation as every prior slice's DB-backed test).
/// </summary>
public class RecordPaymentCommandHandlerFifoAllocationTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly FlatId FlatId = FlatId.New();
    private static readonly BuildingId BuildingId = BuildingId.New();
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;
    private static readonly DateOnly BusinessDate = DateOnly.FromDateTime(DateTime.UtcNow);

    private readonly IResidentAccountRepository _accounts = Substitute.For<IResidentAccountRepository>();
    private readonly IPaymentRepository _payments = Substitute.For<IPaymentRepository>();
    private readonly IInvoiceRepository _invoices = Substitute.For<IInvoiceRepository>();
    private readonly IPaymentAllocationRepository _allocations = Substitute.For<IPaymentAllocationRepository>();
    private readonly ILedgerPostingRepository _ledgerPostings = Substitute.For<ILedgerPostingRepository>();
    private readonly ILedgerEntryRepository _ledgerEntries = Substitute.For<ILedgerEntryRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUserProvider _currentUser = Substitute.For<ICurrentUserProvider>();
    private readonly IClock _clock = Substitute.For<IClock>();

    public RecordPaymentCommandHandlerFifoAllocationTests()
    {
        _currentUser.TenantId.Returns(TenantId);
        _clock.UtcNow.Returns(Now);
        _accounts.GetByFlatIdAsync(TenantId, FlatId, Arg.Any<CancellationToken>())
            .Returns(ResidentAccount.Open(TenantId, FlatId, Now));
        _unitOfWork.BeginTransactionAsync(Arg.Any<CancellationToken>())
            .Returns(Substitute.For<IUnitOfWorkTransaction>());
    }

    private RecordPaymentCommandHandler CreateHandler() => new(
        _accounts, _payments, _invoices, _allocations, _ledgerPostings, _ledgerEntries, _unitOfWork,
        _currentUser, _clock, Substitute.For<ILogger<RecordPaymentCommandHandler>>());

    private static Invoice IssueInvoiceWithBalance(string invoiceNumber, decimal amount)
    {
        InvoiceLineInput line = new(
            ServiceChargeRuleId.New(), "Standard Charge", "ServiceCharge", CalculationMethod.FixedAmount, amount,
            null, 1m, amount, "Standard Charge (ServiceCharge)");

        (Invoice invoice, _) = Invoice.Issue(
            TenantId, BuildingId, FlatId, invoiceNumber, BusinessDate, BusinessDate, BusinessDate, BusinessDate,
            [line], LedgerPostingId.New(), Now);

        return invoice;
    }

    [Fact]
    public async Task Allocates_Fully_To_Single_Outstanding_Invoice_When_Payment_Equals_Balance()
    {
        Invoice invoice = IssueInvoiceWithBalance("INV-A-2026-000001", 1000m);
        _invoices.GetOutstandingForFlatForUpdateAsync(TenantId, FlatId, Arg.Any<CancellationToken>())
            .Returns(new List<Invoice> { invoice });

        RecordPaymentCommand command = new(FlatId.Value, 1000m, "Cash", null, BusinessDate, null);
        PaymentDto result = await CreateHandler().Handle(command, CancellationToken.None);

        invoice.Status.Should().Be(InvoiceStatus.Paid);
        invoice.Balance.Should().Be(0m);
        result.Allocations.Should().ContainSingle(a => a.AllocatedAmount == 1000m);
    }

    [Fact]
    public async Task Partial_Payment_Leaves_Invoice_PartiallyPaid()
    {
        Invoice invoice = IssueInvoiceWithBalance("INV-A-2026-000001", 1000m);
        _invoices.GetOutstandingForFlatForUpdateAsync(TenantId, FlatId, Arg.Any<CancellationToken>())
            .Returns(new List<Invoice> { invoice });

        RecordPaymentCommand command = new(FlatId.Value, 400m, "Cash", null, BusinessDate, null);
        PaymentDto result = await CreateHandler().Handle(command, CancellationToken.None);

        invoice.Status.Should().Be(InvoiceStatus.PartiallyPaid);
        invoice.AmountPaid.Should().Be(400m);
        invoice.Balance.Should().Be(600m);
        result.Allocations.Should().ContainSingle(a => a.AllocatedAmount == 400m);
    }

    [Fact]
    public async Task Allocates_Across_Multiple_Invoices_In_Repository_Order_Oldest_First()
    {
        Invoice older = IssueInvoiceWithBalance("INV-A-2026-000001", 500m);
        Invoice newer = IssueInvoiceWithBalance("INV-A-2026-000002", 800m);
        _invoices.GetOutstandingForFlatForUpdateAsync(TenantId, FlatId, Arg.Any<CancellationToken>())
            .Returns(new List<Invoice> { older, newer });

        RecordPaymentCommand command = new(FlatId.Value, 600m, "Cash", null, BusinessDate, null);
        PaymentDto result = await CreateHandler().Handle(command, CancellationToken.None);

        older.Status.Should().Be(InvoiceStatus.Paid);
        older.Balance.Should().Be(0m);
        newer.Status.Should().Be(InvoiceStatus.PartiallyPaid);
        newer.Balance.Should().Be(700m);
        result.Allocations.Should().HaveCount(2);
        result.Allocations.Sum(a => a.AllocatedAmount).Should().Be(600m);
    }

    [Fact]
    public async Task Overpayment_Pays_Off_All_Outstanding_Invoices_And_Leaves_Remainder_Unallocated()
    {
        Invoice invoiceA = IssueInvoiceWithBalance("INV-A-2026-000001", 500m);
        Invoice invoiceB = IssueInvoiceWithBalance("INV-A-2026-000002", 800m);
        _invoices.GetOutstandingForFlatForUpdateAsync(TenantId, FlatId, Arg.Any<CancellationToken>())
            .Returns(new List<Invoice> { invoiceA, invoiceB });

        RecordPaymentCommand command = new(FlatId.Value, 1500m, "Cash", null, BusinessDate, null);
        PaymentDto result = await CreateHandler().Handle(command, CancellationToken.None);

        invoiceA.Status.Should().Be(InvoiceStatus.Paid);
        invoiceB.Status.Should().Be(InvoiceStatus.Paid);
        result.Allocations.Sum(a => a.AllocatedAmount).Should().Be(1300m);
        // The remaining 200 is not force-allocated anywhere — it stays as unapplied credit,
        // already representable via the flat's net ledger balance going negative.
        result.Amount.Should().Be(1500m);
    }

    [Fact]
    public async Task No_Outstanding_Invoices_Produces_No_Allocations()
    {
        _invoices.GetOutstandingForFlatForUpdateAsync(TenantId, FlatId, Arg.Any<CancellationToken>())
            .Returns(new List<Invoice>());

        RecordPaymentCommand command = new(FlatId.Value, 500m, "Cash", null, BusinessDate, null);
        PaymentDto result = await CreateHandler().Handle(command, CancellationToken.None);

        result.Allocations.Should().BeEmpty();
    }
}
