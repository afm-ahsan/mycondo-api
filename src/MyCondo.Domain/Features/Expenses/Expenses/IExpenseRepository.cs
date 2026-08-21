using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Expenses.ExpenseCategories;
using MyCondo.Domain.Features.Expenses.ExpenseTypes;
using MyCondo.Domain.Features.Finance.Funds;
using MyCondo.Domain.Features.Property.Buildings;

namespace MyCondo.Domain.Features.Expenses.Expenses;

/// <summary>One Expense Category's share of posted expense activity within a period — the Financial
/// Overview report's expense-composition breakdown. <see cref="ExpenseCategoryId"/> is null for
/// expense types with no category on record (pre-Template-3 backfill gap — see
/// <see cref="ExpenseTypes.ExpenseType.ExpenseCategoryId"/>'s doc comment); <see cref="CategoryName"/>
/// is "Uncategorized" in that case.</summary>
public sealed record ExpenseCategoryActivityLine(ExpenseCategoryId? ExpenseCategoryId, string CategoryName, decimal Total);

/// <summary>One Expense Type's share of posted expense activity within a period — same posted-only,
/// AccountingDate-scoped population as <see cref="ExpenseCategoryActivityLine"/>, grouped one level
/// lower (by Type instead of Category), for the Expense by Type report. Carries the owning Category too
/// so callers don't need a second lookup.</summary>
public sealed record ExpenseTypeActivityLine(
    ExpenseTypeId ExpenseTypeId, string TypeName, ExpenseCategoryId? ExpenseCategoryId, string CategoryName,
    int Count, decimal Total);

/// <summary>One <see cref="Expenses.ExpenseStatus"/>'s count/total for a period — a source-record
/// aggregate (not ledger-derived), used only as the Expense Summary report's status breakdown and
/// cross-check total; the ledger-authoritative total for the same period comes from posted
/// <c>LedgerEntry</c> rows on the OperatingExpense account (see <c>IFinanceReportRepository</c>).
/// </summary>
public sealed record ExpenseStatusTotal(ExpenseStatus Status, int Count, decimal TotalAmount);

public interface IExpenseRepository
{
    Task<Expense?> GetByIdAsync(ExpenseId id, CancellationToken cancellationToken);

    Task<bool> ExistsForExpenseTypeAsync(Guid tenantId, ExpenseTypeId expenseTypeId, CancellationToken cancellationToken);

    Task<PagedResult<Expense>> SearchAsync(
        Guid tenantId,
        BuildingId? buildingId,
        ExpenseTypeId? expenseTypeId,
        FundId? fundId,
        ExpenseStatus? status,
        DateOnly? fromDate,
        DateOnly? toDate,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    /// <summary>Posted expense activity (<see cref="ExpenseStatus.Posted"/> or <see cref="ExpenseStatus.Paid"/>
    /// only — <see cref="ExpenseStatus.Recorded"/> has no ledger consequence yet and <see cref="ExpenseStatus.Voided"/>
    /// has been reversed) grouped by Expense Category, by <see cref="Expense.AccountingDate"/> in
    /// [fromDate, toDate] — the source for the Financial Overview report's expense-composition
    /// breakdown, since the ledger's single tenant-wide OperatingExpense account cannot itself
    /// distinguish categories (see <see cref="ExpenseTypes.ExpenseType"/>'s doc comment).</summary>
    Task<IReadOnlyList<ExpenseCategoryActivityLine>> GetExpenseCompositionByCategoryAsync(
        Guid tenantId, DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken);

    /// <summary>Same posted-expense population as <see cref="GetExpenseCompositionByCategoryAsync"/>,
    /// grouped by Expense Type instead of Category — the Expense by Type report's breakdown.</summary>
    Task<IReadOnlyList<ExpenseTypeActivityLine>> GetExpenseCompositionByTypeAsync(
        Guid tenantId, DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken);

    /// <summary>Count/total grouped by <see cref="ExpenseStatus"/> for every expense (any status) whose
    /// <see cref="Expense.AccountingDate"/> falls in [fromDate, toDate] — the Expense Summary report's
    /// status breakdown and source-record cross-check total (current-snapshot Status, not ledger-derived
    /// — see <see cref="ExpenseStatusTotal"/>).</summary>
    Task<IReadOnlyList<ExpenseStatusTotal>> GetStatusTotalsForPeriodAsync(
        Guid tenantId, DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken);

    void Add(Expense expense);
}
