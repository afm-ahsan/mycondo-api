using MyCondo.Domain.Common;

namespace MyCondo.Domain.Features.Expenses.ExpenseCategories;

public interface IExpenseCategoryRepository
{
    Task<ExpenseCategory?> GetByIdAsync(ExpenseCategoryId id, CancellationToken cancellationToken);

    Task<bool> ExistsByCodeAsync(Guid tenantId, string code, ExpenseCategoryId? excludingId, CancellationToken cancellationToken);

    Task<bool> ExistsByNameAsync(Guid tenantId, string name, ExpenseCategoryId? excludingId, CancellationToken cancellationToken);

    Task<PagedResult<ExpenseCategory>> SearchAsync(
        Guid tenantId,
        string? search,
        bool? isActive,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    /// <summary>Unpaginated — the Expense Type create/edit form's category picker needs every active
    /// category, not one page of them (mirrors <c>IExpenseTypeRepository.GetAllActiveForTenantAsync</c>).</summary>
    Task<List<ExpenseCategory>> GetAllActiveForTenantAsync(Guid tenantId, CancellationToken cancellationToken);

    /// <summary>Unpaginated, active and inactive — used by <c>ExpenseCategoryCatalogueSeeder</c> to
    /// reconcile the default catalogue by <c>Code</c>.</summary>
    Task<List<ExpenseCategory>> GetAllForTenantAsync(Guid tenantId, CancellationToken cancellationToken);

    void Add(ExpenseCategory expenseCategory);
}
