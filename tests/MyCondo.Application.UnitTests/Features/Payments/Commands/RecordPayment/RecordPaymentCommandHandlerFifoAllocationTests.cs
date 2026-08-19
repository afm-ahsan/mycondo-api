using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Finance.Services;
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
    private readonly IFinancialPostingService _financialPosting = Substitute.For<IFinancialPostingService>();
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
        StubFinancialPosting();
    }

    /// <summary>Makes the mocked <see cref="IFinancialPostingService"/> behave like the real one — see
    /// VoidInvoiceCommandHandlerTests for why.</summary>
    private void StubFinancialPosting() =>
        _financialPosting.PostAsync(Arg.Any<FinancialPostingRequest>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                FinancialPostingRequest request = callInfo.Arg<FinancialPostingRequest>();
                List<LedgerLine> lines = request.Lines
                    .Select(l => new LedgerLine(l.Role, l.FlatId, l.Direction, l.Amount, l.LineDescription ?? request.Description))
                    .ToList();
                (LedgerPosting posting, IReadOnlyList<LedgerEntry> entries) = LedgerPosting.Create(
                    request.TenantId, request.BusinessDate, request.Description, request.PostingPurpose,
                    request.SourceId, lines, Now);
                return new FinancialPostingResult(posting, entries);
            });

    private RecordPaymentCommandHandler CreateHandler() => new(
        _accounts, _payments, _invoices, _allocations, _financialPosting, _unitOfWork,
        _currentUser, _clock, Substitute.For<ILogger<RecordPaymentCommandHandler>>());

    private static Invoice IssueInvoiceWithBalance(string invoiceNumber, decimal amount)
    {
        InvoiceLineInput line = new(
            ServiceChargeRuleId.New(), "Standard Charge", "ServiceCharge", "FixedAmount", amount,
            null, 1m, amount, "Standard Charge (ServiceCharge)");

        (Invoice invoice, _) = Invoice.Issue(
            TenantId, BuildingId, FlatId, invoiceNumber, InvoiceSource.ServiceCharge, BusinessDate, BusinessDate,
            BusinessDate, BusinessDate, [line], LedgerPostingId.New(), LedgerAccountType.AssociationRevenue, null, Now);

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
    public async Task Overpayment_Pays_Off_All_Outstanding_Invoices_And_Posts_Remainder_As_ResidentAdvance()
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
        result.Amount.Should().Be(1500m);

        // The remaining 200 is posted as ResidentAdvance (Billing↔Finance integration template §12),
        // not left as unapplied credit implicit in a negative ResidentReceivable balance.
        await _financialPosting.Received(1).PostAsync(
            Arg.Is<FinancialPostingRequest>(r => r.Lines.Any(l =>
                l.Role == LedgerAccountType.ResidentAdvance && l.FlatId == FlatId && l.Amount == 200m)),
            Arg.Any<CancellationToken>());
        await _financialPosting.Received(1).PostAsync(
            Arg.Is<FinancialPostingRequest>(r => r.Lines.Any(l =>
                l.Role == LedgerAccountType.ResidentReceivable && l.FlatId == FlatId && l.Amount == 1300m)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Exact_Payment_With_No_Remainder_Does_Not_Post_A_ResidentAdvance_Line()
    {
        Invoice invoice = IssueInvoiceWithBalance("INV-A-2026-000001", 1000m);
        _invoices.GetOutstandingForFlatForUpdateAsync(TenantId, FlatId, Arg.Any<CancellationToken>())
            .Returns(new List<Invoice> { invoice });

        RecordPaymentCommand command = new(FlatId.Value, 1000m, "Cash", null, BusinessDate, null);
        await CreateHandler().Handle(command, CancellationToken.None);

        await _financialPosting.Received(1).PostAsync(
            Arg.Is<FinancialPostingRequest>(r => r.Lines.All(l => l.Role != LedgerAccountType.ResidentAdvance)),
            Arg.Any<CancellationToken>());
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
