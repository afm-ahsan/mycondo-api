using MyCondo.Application.Features.Expenses.Expenses.DTOs;
using MyCondo.Domain.Features.Expenses.Expenses;

namespace MyCondo.Application.Features.Expenses.Expenses.Mappings;

internal static class ExpenseMappings
{
    /// <summary>Names/ids resolved by the caller (a lookup across Building/ExpenseType/ExpenseCategory/
    /// Fund repositories) since <see cref="Expense"/> itself only stores <c>ExpenseTypeId</c> directly —
    /// <c>ExpenseCategoryId</c> is one hop away, resolved via the type's own category reference. Same
    /// "handler resolves, mapping just shapes" split every other Expense/ExpenseType handler already
    /// uses.</summary>
    public static ExpenseDto ToDto(
        this Expense expense, string? buildingName, string expenseTypeName, Guid? expenseCategoryId,
        string? expenseCategoryName, string? fundName) => new(
        expense.Id.Value, expense.BuildingId?.Value, buildingName, expense.ExpenseTypeId.Value, expenseTypeName,
        expenseCategoryId, expenseCategoryName, expense.FundId?.Value, fundName, expense.ExpenseDate,
        expense.AccountingDate, expense.Description, expense.Payee, expense.ReferenceNumber, expense.Amount,
        expense.IsPaid, expense.PaymentMethod.ToString(), expense.Notes, expense.Status.ToString(),
        expense.VoidReason, expense.FinancialAccountId?.Value, expense.PostingId?.Value,
        expense.PaymentPostingId?.Value, expense.ReversalPostingId?.Value, expense.PaymentReversalPostingId?.Value,
        expense.CreatedBy, expense.CreatedAtUtc, expense.UpdatedAtUtc);
}
