using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Expenses.Expenses.Commands.ApproveExpense;
using MyCondo.Application.Features.Finance.Services;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Expenses.ExpenseCategories;
using MyCondo.Domain.Features.Expenses.Expenses;
using MyCondo.Domain.Features.Expenses.ExpenseTypes;
using MyCondo.Domain.Features.Finance.Audit;
using MyCondo.Domain.Features.Finance.ChartOfAccounts;
using MyCondo.Domain.Features.Finance.Funds;
using MyCondo.Domain.Features.Payments.Ledger;
using MyCondo.Domain.Features.Payments.Payments;
using MyCondo.Domain.Features.Property.Buildings;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Expenses.Expenses.Commands.ApproveExpense;

/// <summary>
/// Proves the primary Expense posting: role selection driven by <see cref="Expense.IsPaid"/>, and —
/// the Template 5 closure focus — that <see cref="Expense.FundId"/> (Template 3) reaches
/// <see cref="FinancialPostingRequest.FundId"/> unchanged, so it lands on every resulting
/// <c>LedgerEntry</c> and the Fund Position report can attribute this posting correctly.
/// </summary>
public class ApproveExpenseCommandHandlerTests
{
    private static readonly DateTimeOffset NowUtc = new(2026, 8, 11, 0, 0, 0, TimeSpan.Zero);
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid OtherTenantId = Guid.NewGuid();
    private static readonly BuildingId ABuildingId = new(Guid.NewGuid());

    private readonly IExpenseRepository _expenses = Substitute.For<IExpenseRepository>();
    private readonly IExpenseTypeRepository _expenseTypes = Substitute.For<IExpenseTypeRepository>();
    private readonly IExpenseCategoryRepository _expenseCategories = Substitute.For<IExpenseCategoryRepository>();
    private readonly IBuildingRepository _buildings = Substitute.For<IBuildingRepository>();
    private readonly IFundRepository _funds = Substitute.For<IFundRepository>();
    private readonly IFinancialPostingService _financialPosting = Substitute.For<IFinancialPostingService>();
    private readonly IFinanceAuditLogRepository _auditLog = Substitute.For<IFinanceAuditLogRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUserProvider _currentUser = Substitute.For<ICurrentUserProvider>();
    private readonly IClock _clock = Substitute.For<IClock>();

    public ApproveExpenseCommandHandlerTests()
    {
        _currentUser.TenantId.Returns(TenantId);
        _currentUser.HasPermissionForBuilding(Arg.Any<string>(), Arg.Any<Guid?>()).Returns(true);
        _clock.UtcNow.Returns(NowUtc);
        StubFinancialPosting();
    }

    /// <summary>Makes the mocked <see cref="IFinancialPostingService"/> behave like the real one — same
    /// helper shape as <c>VoidExpenseCommandHandlerTests</c> — so the resulting entries actually carry
    /// the requested <see cref="FinancialPostingRequest.FundId"/>, proving propagation end-to-end rather
    /// than only asserting on the outgoing request.</summary>
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

    private ApproveExpenseCommandHandler CreateHandler() => new(
        _expenses, _expenseTypes, _expenseCategories, _buildings, _funds, _financialPosting, _auditLog, _unitOfWork,
        _currentUser, _clock, Substitute.For<ILogger<ApproveExpenseCommandHandler>>());

    private static Expense RecordExpense(Guid tenantId, FundId? fundId, bool isPaid) => Expense.Record(
        tenantId, ABuildingId, new ExpenseTypeId(Guid.NewGuid()), fundId, new DateOnly(2026, 8, 1),
        accountingDate: null, "Cleaning", null, null, 1000m, isPaid, PaymentMethod.Cash, null, NowUtc);

    [Fact]
    public async Task Stamps_The_Expenses_FundId_Onto_Every_Resulting_Ledger_Entry()
    {
        Fund fund = Fund.Create(TenantId, "RSV", "Reserve Fund", null);
        Expense expense = RecordExpense(TenantId, fund.Id, isPaid: false);
        _expenses.GetByIdAsync(expense.Id, Arg.Any<CancellationToken>()).Returns(expense);
        _funds.GetByIdAsync(fund.Id, Arg.Any<CancellationToken>()).Returns(fund);

        await CreateHandler().Handle(new ApproveExpenseCommand(expense.Id.Value), CancellationToken.None);

        await _financialPosting.Received(1).PostAsync(
            Arg.Is<FinancialPostingRequest>(r => r.FundId == fund.Id), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Leaves_FundId_Null_When_The_Expense_Has_No_Fund()
    {
        Expense expense = RecordExpense(TenantId, fundId: null, isPaid: false);
        _expenses.GetByIdAsync(expense.Id, Arg.Any<CancellationToken>()).Returns(expense);

        await CreateHandler().Handle(new ApproveExpenseCommand(expense.Id.Value), CancellationToken.None);

        await _financialPosting.Received(1).PostAsync(
            Arg.Is<FinancialPostingRequest>(r => r.FundId == null), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Posts_Dr_Expense_Cr_AccountsPayable_When_Recorded_Unpaid()
    {
        Expense expense = RecordExpense(TenantId, fundId: null, isPaid: false);
        _expenses.GetByIdAsync(expense.Id, Arg.Any<CancellationToken>()).Returns(expense);

        await CreateHandler().Handle(new ApproveExpenseCommand(expense.Id.Value), CancellationToken.None);

        await _financialPosting.Received(1).PostAsync(
            Arg.Is<FinancialPostingRequest>(r =>
                r.Lines.Any(l => l.Role == LedgerAccountType.OperatingExpense && l.Direction == LedgerDirection.Debit && l.Amount == 1000m) &&
                r.Lines.Any(l => l.Role == LedgerAccountType.AccountsPayable && l.Direction == LedgerDirection.Credit && l.Amount == 1000m)),
            Arg.Any<CancellationToken>());
        expense.FinancialAccountId.Should().BeNull();
    }

    [Fact]
    public async Task Posts_Dr_Expense_Cr_CashOrBank_When_Paid_Immediately()
    {
        Expense expense = RecordExpense(TenantId, fundId: null, isPaid: true);
        _expenses.GetByIdAsync(expense.Id, Arg.Any<CancellationToken>()).Returns(expense);

        await CreateHandler().Handle(new ApproveExpenseCommand(expense.Id.Value), CancellationToken.None);

        await _financialPosting.Received(1).PostAsync(
            Arg.Is<FinancialPostingRequest>(r =>
                r.Lines.Any(l => l.Role == LedgerAccountType.OperatingExpense && l.Direction == LedgerDirection.Debit && l.Amount == 1000m) &&
                r.Lines.Any(l => l.Role == LedgerAccountType.CashOrBank && l.Direction == LedgerDirection.Credit && l.Amount == 1000m)),
            Arg.Any<CancellationToken>());
        expense.FinancialAccountId.Should().NotBeNull();
        expense.Status.Should().Be(ExpenseStatus.Posted);
    }

    [Fact]
    public async Task Throws_Conflict_When_The_Expense_Is_Not_Recorded()
    {
        Expense expense = RecordExpense(TenantId, fundId: null, isPaid: false);
        expense.MarkPosted(LedgerPostingId.New(), null, NowUtc);
        _expenses.GetByIdAsync(expense.Id, Arg.Any<CancellationToken>()).Returns(expense);

        Func<Task> act = async () => await CreateHandler()
            .Handle(new ApproveExpenseCommand(expense.Id.Value), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
        await _financialPosting.DidNotReceive().PostAsync(Arg.Any<FinancialPostingRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Throws_NotFound_When_Expense_Belongs_To_A_Different_Tenant()
    {
        Expense expense = RecordExpense(OtherTenantId, fundId: null, isPaid: false);
        _expenses.GetByIdAsync(expense.Id, Arg.Any<CancellationToken>()).Returns(expense);

        Func<Task> act = async () => await CreateHandler()
            .Handle(new ApproveExpenseCommand(expense.Id.Value), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Throws_Forbidden_When_Caller_Lacks_Permission_For_The_Buildings_Scope()
    {
        Expense expense = RecordExpense(TenantId, fundId: null, isPaid: false);
        _expenses.GetByIdAsync(expense.Id, Arg.Any<CancellationToken>()).Returns(expense);
        _currentUser.HasPermissionForBuilding("expense.approve", ABuildingId.Value).Returns(false);

        Func<Task> act = async () => await CreateHandler()
            .Handle(new ApproveExpenseCommand(expense.Id.Value), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
