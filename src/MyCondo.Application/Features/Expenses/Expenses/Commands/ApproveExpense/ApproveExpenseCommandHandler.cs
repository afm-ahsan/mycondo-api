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

namespace MyCondo.Application.Features.Expenses.Expenses.Commands.ApproveExpense;

/// <summary>
/// Approves an Expense and posts its primary accounting consequence through the centralized
/// <see cref="IFinancialPostingService"/> — "Dr OperatingExpense / Cr AccountsPayable" when recorded
/// unpaid, or "Dr OperatingExpense / Cr CashOrBank" when recorded as an immediate payment. Idempotent:
/// <c>Expense.Id</c> is the posting's <c>SourceId</c>, so a retried/duplicate call either 409s at
/// the posting service (see <c>MissingAccountMappingException</c>'s sibling <c>ConflictException</c> path
/// in <c>FinancialPostingService</c>) or is rejected earlier by <see cref="Expense.MarkPosted"/>'s
/// status guard once the first call has already advanced the Expense past <see cref="ExpenseStatus.Recorded"/>.
/// </summary>
public sealed class ApproveExpenseCommandHandler(
    IExpenseRepository expenses,
    IExpenseTypeRepository expenseTypes,
    IExpenseCategoryRepository expenseCategories,
    IBuildingRepository buildings,
    IFundRepository funds,
    IFinancialPostingService financialPosting,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    IClock clock,
    ILogger<ApproveExpenseCommandHandler> logger
) : IRequestHandler<ApproveExpenseCommand, ExpenseDto>
{
    private const string ExpenseApprovePermission = "expense.approve";

    public async ValueTask<ExpenseDto> Handle(ApproveExpenseCommand command, CancellationToken cancellationToken)
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

        if (!currentUser.HasPermissionForBuilding(ExpenseApprovePermission, expense.BuildingId?.Value))
        {
            throw new ForbiddenException("You do not have permission to approve expenses for this Building.");
        }

        if (expense.Status != ExpenseStatus.Recorded)
        {
            throw new ConflictException($"Expense {expense.Id} is {expense.Status} and cannot be approved.");
        }

        LedgerAccountType creditRole = expense.IsPaid ? LedgerAccountType.CashOrBank : LedgerAccountType.AccountsPayable;
        FinancialPostingLine[] postingLines =
        [
            new FinancialPostingLine(LedgerAccountType.OperatingExpense, null, LedgerDirection.Debit, expense.Amount),
            new FinancialPostingLine(creditRole, null, LedgerDirection.Credit, expense.Amount),
        ];

        FinancialPostingResult posted = await financialPosting.PostAsync(
            new FinancialPostingRequest(
                tenantId, expense.AccountingDate, $"Expense: {expense.Description}", "ExpenseRecording",
                expenseId.Value, postingLines, expense.FundId),
            cancellationToken);

        ChartOfAccountId? financialAccountId = expense.IsPaid
            ? posted.Entries.First(e => e.AccountType == LedgerAccountType.CashOrBank).ChartOfAccountId
            : null;

        expense.MarkPosted(posted.Posting.Id, financialAccountId, clock.UtcNow);
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
            "Expense {ExpenseId} approved/posted for tenant {TenantId}, posting {PostingId}",
            expenseId, tenantId, posted.Posting.Id);

        return expense.ToDto(
            building?.Name, expenseType?.Name ?? "Unknown", expenseCategory?.Id.Value, expenseCategory?.Name,
            fund?.Name);
    }
}
