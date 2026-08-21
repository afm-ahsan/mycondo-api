using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Expenses.Expenses.DTOs;
using MyCondo.Application.Features.Expenses.Expenses.Mappings;
using MyCondo.Application.Features.Finance.Services;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Expenses.ExpenseCategories;
using MyCondo.Domain.Features.Expenses.Expenses;
using MyCondo.Domain.Features.Expenses.ExpenseTypes;
using MyCondo.Domain.Features.Finance.ChartOfAccounts;
using MyCondo.Domain.Features.Finance.Funds;
using MyCondo.Domain.Features.Payments.Ledger;
using MyCondo.Domain.Features.Property.Buildings;

namespace MyCondo.Application.Features.Expenses.Expenses.Commands.PayExpense;

/// <summary>
/// Records a supplier payment against a previously unpaid, posted Expense — "Dr AccountsPayable / Cr
/// CashOrBank" — through the centralized <see cref="IFinancialPostingService"/>. Idempotent the same way
/// <c>ApproveExpenseCommandHandler</c> is: <c>Expense.Id</c> is the posting's <c>SourceId</c>, but
/// under a distinct <c>PostingPurpose</c> ("ExpensePayment" vs. "ExpenseRecording") so both postings for
/// the same Expense coexist under the database's (TenantId, ReferenceType, ReferenceId) uniqueness rule.
/// </summary>
public sealed class PayExpenseCommandHandler(
    IExpenseRepository expenses,
    IExpenseTypeRepository expenseTypes,
    IExpenseCategoryRepository expenseCategories,
    IBuildingRepository buildings,
    IFundRepository funds,
    IFinancialPostingService financialPosting,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    IClock clock,
    ILogger<PayExpenseCommandHandler> logger
) : IRequestHandler<PayExpenseCommand, ExpenseDto>
{
    private const string ExpensePayPermission = "expense.pay";

    public async ValueTask<ExpenseDto> Handle(PayExpenseCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        ExpenseId expenseId = new(command.ExpenseId);
        Expense expense = await expenses.GetByIdAsync(expenseId, cancellationToken)
            ?? throw new NotFoundException(nameof(Expense), command.ExpenseId);

        if (expense.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(Expense), command.ExpenseId);
        }

        if (!currentUser.HasPermissionForBuilding(ExpensePayPermission, expense.BuildingId?.Value))
        {
            throw new ForbiddenException("You do not have permission to pay expenses for this Building.");
        }

        if (expense.Status != ExpenseStatus.Posted || expense.IsPaid)
        {
            throw new ConflictException(
                $"Expense {expense.Id} is {expense.Status}{(expense.IsPaid ? " and already paid" : "")} and cannot be paid.");
        }

        DateOnly businessDate = DateOnly.FromDateTime(clock.UtcNow.UtcDateTime);
        FinancialPostingLine[] postingLines =
        [
            new FinancialPostingLine(LedgerAccountType.AccountsPayable, null, LedgerDirection.Debit, expense.Amount),
            new FinancialPostingLine(LedgerAccountType.CashOrBank, null, LedgerDirection.Credit, expense.Amount),
        ];

        FinancialPostingResult posted = await financialPosting.PostAsync(
            new FinancialPostingRequest(
                tenantId, businessDate, $"Supplier payment for Expense: {expense.Description}", "ExpensePayment",
                expenseId.Value, postingLines, expense.FundId),
            cancellationToken);

        ChartOfAccountId financialAccountId = posted.Entries.First(e => e.AccountType == LedgerAccountType.CashOrBank).ChartOfAccountId!.Value;

        expense.MarkPaid(posted.Posting.Id, financialAccountId, clock.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        Building? building = expense.BuildingId is BuildingId buildingId
            ? await buildings.GetByIdAsync(buildingId, cancellationToken)
            : null;
        ExpenseType? expenseType = await expenseTypes.GetByIdAsync(expense.ExpenseTypeId, cancellationToken);
        ExpenseCategory? expenseCategory = expenseType?.ExpenseCategoryId is ExpenseCategoryId categoryId
            ? await expenseCategories.GetByIdAsync(categoryId, cancellationToken)
            : null;
        Fund? fund = expense.FundId is FundId fundId ? await funds.GetByIdAsync(fundId, cancellationToken) : null;

        logger.LogInformation(
            "Expense {ExpenseId} paid for tenant {TenantId}, payment posting {PostingId}",
            expenseId, tenantId, posted.Posting.Id);

        return expense.ToDto(
            building?.Name, expenseType?.Name ?? "Unknown", expenseCategory?.Id.Value, expenseCategory?.Name,
            fund?.Name);
    }
}
