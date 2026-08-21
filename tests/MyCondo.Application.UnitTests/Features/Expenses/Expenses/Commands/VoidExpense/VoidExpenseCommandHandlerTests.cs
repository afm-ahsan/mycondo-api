using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Expenses.Expenses.Commands.VoidExpense;
using MyCondo.Application.Features.Finance.Services;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Expenses.Expenses;
using MyCondo.Domain.Features.Expenses.ExpenseTypes;
using MyCondo.Domain.Features.Finance.Audit;
using MyCondo.Domain.Features.Finance.ChartOfAccounts;
using MyCondo.Domain.Features.Payments.Ledger;
using MyCondo.Domain.Features.Payments.Payments;
using MyCondo.Domain.Features.Property.Buildings;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Expenses.Expenses.Commands.VoidExpense;

/// <summary>
/// Proves the append-only ledger invariant at the handler level, same rationale as
/// <c>VoidInvoiceCommandHandlerTests</c>: a Recorded-but-never-posted Expense voids with no ledger
/// effect at all, while a Posted/Paid Expense's void always asks <see cref="IFinancialPostingService"/>
/// for brand-new reversing posting(s) — never mutates the original posting.
/// </summary>
public class VoidExpenseCommandHandlerTests
{
    private static readonly DateTimeOffset NowUtc = new(2026, 8, 11, 0, 0, 0, TimeSpan.Zero);
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid OtherTenantId = Guid.NewGuid();
    private static readonly BuildingId ABuildingId = new(Guid.NewGuid());

    private readonly IExpenseRepository _expenses = Substitute.For<IExpenseRepository>();
    private readonly IFinancialPostingService _financialPosting = Substitute.For<IFinancialPostingService>();
    private readonly IFinanceAuditLogRepository _auditLog = Substitute.For<IFinanceAuditLogRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUserProvider _currentUser = Substitute.For<ICurrentUserProvider>();
    private readonly IClock _clock = Substitute.For<IClock>();

    public VoidExpenseCommandHandlerTests()
    {
        _currentUser.TenantId.Returns(TenantId);
        _currentUser.HasPermissionForBuilding(Arg.Any<string>(), Arg.Any<Guid?>()).Returns(true);
        _clock.UtcNow.Returns(NowUtc);
        StubFinancialPosting();
    }

    /// <summary>Makes the mocked <see cref="IFinancialPostingService"/> behave like the real one — see
    /// <c>VoidInvoiceCommandHandlerTests</c>'s identical helper.</summary>
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
                    request.SourceId, lines, NowUtc);
                foreach (LedgerEntry entry in entries)
                {
                    entry.SetFinanceDimensions(ChartOfAccountId.New(), request.FundId, null);
                }
                return new FinancialPostingResult(posting, entries);
            });

    private VoidExpenseCommandHandler CreateHandler() => new(
        _expenses, _financialPosting, _auditLog, _unitOfWork, _currentUser, _clock,
        Substitute.For<ILogger<VoidExpenseCommandHandler>>());

    private static Expense RecordExpense(Guid tenantId, bool isPaid = false) => Expense.Record(
        tenantId, ABuildingId, new ExpenseTypeId(Guid.NewGuid()), fundId: null, new DateOnly(2026, 8, 1),
        accountingDate: null, "Cleaning", null, null, 1000m, isPaid, PaymentMethod.Cash, null, NowUtc);

    [Fact]
    public async Task Voids_A_Never_Posted_Expense_With_No_Ledger_Effect()
    {
        Expense expense = RecordExpense(TenantId);
        _expenses.GetByIdAsync(expense.Id, Arg.Any<CancellationToken>()).Returns(expense);

        await CreateHandler().Handle(new VoidExpenseCommand(expense.Id.Value, "Recorded in error"), CancellationToken.None);

        expense.Status.Should().Be(ExpenseStatus.Voided);
        expense.VoidReason.Should().Be("Recorded in error");
        expense.ReversalPostingId.Should().BeNull();
        await _financialPosting.DidNotReceive().PostAsync(Arg.Any<FinancialPostingRequest>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Voids_A_Posted_Unpaid_Expense_By_Reversing_Dr_Expense_Cr_AccountsPayable()
    {
        Expense expense = RecordExpense(TenantId, isPaid: false);
        LedgerPostingId postingId = LedgerPostingId.New();
        expense.MarkPosted(postingId, null, NowUtc);
        _expenses.GetByIdAsync(expense.Id, Arg.Any<CancellationToken>()).Returns(expense);

        await CreateHandler().Handle(new VoidExpenseCommand(expense.Id.Value, "Correction"), CancellationToken.None);

        expense.Status.Should().Be(ExpenseStatus.Voided);
        expense.ReversalPostingId.Should().NotBeNull();

        await _financialPosting.Received(1).PostAsync(
            Arg.Is<FinancialPostingRequest>(r =>
                r.PostingPurpose == "ExpenseVoid" &&
                r.SourceId == postingId.Value &&
                r.Lines.Count == 2 &&
                r.Lines.Any(l => l.Role == LedgerAccountType.AccountsPayable && l.Direction == LedgerDirection.Debit && l.Amount == 1000m) &&
                r.Lines.Any(l => l.Role == LedgerAccountType.OperatingExpense && l.Direction == LedgerDirection.Credit && l.Amount == 1000m)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Voids_A_Posted_Immediately_Paid_Expense_By_Reversing_Dr_Expense_Cr_CashOrBank()
    {
        Expense expense = RecordExpense(TenantId, isPaid: true);
        LedgerPostingId postingId = LedgerPostingId.New();
        expense.MarkPosted(postingId, ChartOfAccountId.New(), NowUtc);
        _expenses.GetByIdAsync(expense.Id, Arg.Any<CancellationToken>()).Returns(expense);

        await CreateHandler().Handle(new VoidExpenseCommand(expense.Id.Value, "Correction"), CancellationToken.None);

        await _financialPosting.Received(1).PostAsync(
            Arg.Is<FinancialPostingRequest>(r =>
                r.PostingPurpose == "ExpenseVoid" &&
                r.Lines.Any(l => l.Role == LedgerAccountType.CashOrBank && l.Direction == LedgerDirection.Debit && l.Amount == 1000m) &&
                r.Lines.Any(l => l.Role == LedgerAccountType.OperatingExpense && l.Direction == LedgerDirection.Credit && l.Amount == 1000m)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Voids_A_Paid_Expense_By_Reversing_Both_The_Recording_And_The_Supplier_Payment()
    {
        Expense expense = RecordExpense(TenantId, isPaid: false);
        expense.MarkPosted(LedgerPostingId.New(), null, NowUtc);
        LedgerPostingId paymentPostingId = LedgerPostingId.New();
        expense.MarkPaid(paymentPostingId, ChartOfAccountId.New(), NowUtc);
        _expenses.GetByIdAsync(expense.Id, Arg.Any<CancellationToken>()).Returns(expense);

        await CreateHandler().Handle(new VoidExpenseCommand(expense.Id.Value, "Correction"), CancellationToken.None);

        expense.Status.Should().Be(ExpenseStatus.Voided);
        expense.ReversalPostingId.Should().NotBeNull();
        expense.PaymentReversalPostingId.Should().NotBeNull();

        // Payment reversal: Dr CashOrBank / Cr AccountsPayable (undoing "Dr AccountsPayable / Cr CashOrBank").
        await _financialPosting.Received(1).PostAsync(
            Arg.Is<FinancialPostingRequest>(r =>
                r.PostingPurpose == "ExpensePaymentVoid" &&
                r.SourceId == paymentPostingId.Value &&
                r.Lines.Any(l => l.Role == LedgerAccountType.CashOrBank && l.Direction == LedgerDirection.Debit) &&
                r.Lines.Any(l => l.Role == LedgerAccountType.AccountsPayable && l.Direction == LedgerDirection.Credit)),
            Arg.Any<CancellationToken>());

        // Primary reversal still credits AccountsPayable (the original recording was always
        // unpaid-at-posting-time even though IsPaid has since flipped true) — Dr AccountsPayable / Cr
        // OperatingExpense.
        await _financialPosting.Received(1).PostAsync(
            Arg.Is<FinancialPostingRequest>(r =>
                r.PostingPurpose == "ExpenseVoid" &&
                r.Lines.Any(l => l.Role == LedgerAccountType.AccountsPayable && l.Direction == LedgerDirection.Debit) &&
                r.Lines.Any(l => l.Role == LedgerAccountType.OperatingExpense && l.Direction == LedgerDirection.Credit)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Voiding_An_Already_Voided_Expense_Is_Idempotent_And_Posts_Nothing_Again()
    {
        Expense expense = RecordExpense(TenantId);
        expense.Void("First reason", NowUtc);
        _expenses.GetByIdAsync(expense.Id, Arg.Any<CancellationToken>()).Returns(expense);

        await CreateHandler().Handle(new VoidExpenseCommand(expense.Id.Value, "Second reason"), CancellationToken.None);

        expense.VoidReason.Should().Be("First reason");
        await _financialPosting.DidNotReceive().PostAsync(Arg.Any<FinancialPostingRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Throws_NotFound_When_Expense_Belongs_To_A_Different_Tenant()
    {
        Expense expense = RecordExpense(OtherTenantId);
        _expenses.GetByIdAsync(expense.Id, Arg.Any<CancellationToken>()).Returns(expense);

        Func<Task> act = async () => await CreateHandler()
            .Handle(new VoidExpenseCommand(expense.Id.Value, "Reason"), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Throws_Forbidden_When_Caller_Lacks_Permission_For_The_Buildings_Scope()
    {
        Expense expense = RecordExpense(TenantId);
        _expenses.GetByIdAsync(expense.Id, Arg.Any<CancellationToken>()).Returns(expense);
        _currentUser.HasPermissionForBuilding("expense.manage", ABuildingId.Value).Returns(false);

        Func<Task> act = async () => await CreateHandler()
            .Handle(new VoidExpenseCommand(expense.Id.Value, "Reason"), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }
}
