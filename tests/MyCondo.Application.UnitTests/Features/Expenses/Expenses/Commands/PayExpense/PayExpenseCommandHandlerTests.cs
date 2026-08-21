using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Expenses.Expenses.Commands.PayExpense;
using MyCondo.Application.Features.Finance.Services;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Expenses.ExpenseCategories;
using MyCondo.Domain.Features.Expenses.Expenses;
using MyCondo.Domain.Features.Expenses.ExpenseTypes;
using MyCondo.Domain.Features.Finance.ChartOfAccounts;
using MyCondo.Domain.Features.Finance.Funds;
using MyCondo.Domain.Features.Payments.Ledger;
using MyCondo.Domain.Features.Payments.Payments;
using MyCondo.Domain.Features.Property.Buildings;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Expenses.Expenses.Commands.PayExpense;

/// <summary>
/// Proves the supplier-payment posting ("Dr AccountsPayable / Cr CashOrBank") and — the Template 5
/// closure focus — that <see cref="Expense.FundId"/> also reaches this second, later posting the same
/// way it reaches the primary <c>ApproveExpenseCommandHandler</c> posting.
/// </summary>
public class PayExpenseCommandHandlerTests
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
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUserProvider _currentUser = Substitute.For<ICurrentUserProvider>();
    private readonly IClock _clock = Substitute.For<IClock>();

    public PayExpenseCommandHandlerTests()
    {
        _currentUser.TenantId.Returns(TenantId);
        _currentUser.HasPermissionForBuilding(Arg.Any<string>(), Arg.Any<Guid?>()).Returns(true);
        _clock.UtcNow.Returns(NowUtc);
        StubFinancialPosting();
    }

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

    private PayExpenseCommandHandler CreateHandler() => new(
        _expenses, _expenseTypes, _expenseCategories, _buildings, _funds, _financialPosting, _unitOfWork,
        _currentUser, _clock, Substitute.For<ILogger<PayExpenseCommandHandler>>());

    private static Expense PostedUnpaidExpense(Guid tenantId, FundId? fundId)
    {
        Expense expense = Expense.Record(
            tenantId, ABuildingId, new ExpenseTypeId(Guid.NewGuid()), fundId, new DateOnly(2026, 8, 1),
            accountingDate: null, "Cleaning", null, null, 1000m, isPaid: false, PaymentMethod.Cash, null, NowUtc);
        expense.MarkPosted(LedgerPostingId.New(), null, NowUtc);
        return expense;
    }

    [Fact]
    public async Task Stamps_The_Expenses_FundId_Onto_The_Supplier_Payment_Posting()
    {
        Fund fund = Fund.Create(TenantId, "RSV", "Reserve Fund", null);
        Expense expense = PostedUnpaidExpense(TenantId, fund.Id);
        _expenses.GetByIdAsync(expense.Id, Arg.Any<CancellationToken>()).Returns(expense);
        _funds.GetByIdAsync(fund.Id, Arg.Any<CancellationToken>()).Returns(fund);

        await CreateHandler().Handle(new PayExpenseCommand(expense.Id.Value), CancellationToken.None);

        await _financialPosting.Received(1).PostAsync(
            Arg.Is<FinancialPostingRequest>(r => r.FundId == fund.Id && r.PostingPurpose == "ExpensePayment"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Posts_Dr_AccountsPayable_Cr_CashOrBank_And_Marks_The_Expense_Paid()
    {
        Expense expense = PostedUnpaidExpense(TenantId, fundId: null);
        _expenses.GetByIdAsync(expense.Id, Arg.Any<CancellationToken>()).Returns(expense);

        await CreateHandler().Handle(new PayExpenseCommand(expense.Id.Value), CancellationToken.None);

        await _financialPosting.Received(1).PostAsync(
            Arg.Is<FinancialPostingRequest>(r =>
                r.Lines.Any(l => l.Role == LedgerAccountType.AccountsPayable && l.Direction == LedgerDirection.Debit && l.Amount == 1000m) &&
                r.Lines.Any(l => l.Role == LedgerAccountType.CashOrBank && l.Direction == LedgerDirection.Credit && l.Amount == 1000m)),
            Arg.Any<CancellationToken>());
        expense.IsPaid.Should().BeTrue();
        expense.PaymentPostingId.Should().NotBeNull();
        expense.FinancialAccountId.Should().NotBeNull();
    }

    [Fact]
    public async Task Throws_Conflict_When_The_Expense_Is_Already_Paid()
    {
        Expense expense = PostedUnpaidExpense(TenantId, fundId: null);
        expense.MarkPaid(LedgerPostingId.New(), ChartOfAccountId.New(), NowUtc);
        _expenses.GetByIdAsync(expense.Id, Arg.Any<CancellationToken>()).Returns(expense);

        Func<Task> act = async () => await CreateHandler()
            .Handle(new PayExpenseCommand(expense.Id.Value), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
        await _financialPosting.DidNotReceive().PostAsync(Arg.Any<FinancialPostingRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Throws_NotFound_When_Expense_Belongs_To_A_Different_Tenant()
    {
        Expense expense = PostedUnpaidExpense(OtherTenantId, fundId: null);
        _expenses.GetByIdAsync(expense.Id, Arg.Any<CancellationToken>()).Returns(expense);

        Func<Task> act = async () => await CreateHandler()
            .Handle(new PayExpenseCommand(expense.Id.Value), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Throws_Forbidden_When_Caller_Lacks_Permission_For_The_Buildings_Scope()
    {
        Expense expense = PostedUnpaidExpense(TenantId, fundId: null);
        _expenses.GetByIdAsync(expense.Id, Arg.Any<CancellationToken>()).Returns(expense);
        _currentUser.HasPermissionForBuilding("expense.pay", ABuildingId.Value).Returns(false);

        Func<Task> act = async () => await CreateHandler()
            .Handle(new PayExpenseCommand(expense.Id.Value), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
