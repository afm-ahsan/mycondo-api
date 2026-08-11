using MyCondo.Domain.Common;

namespace MyCondo.Domain.Features.Expenses.ExpenseTypes;

public interface IExpenseTypeRepository
{
    Task<ExpenseType?> GetByIdAsync(ExpenseTypeId id, CancellationToken cancellationToken);

    Task<bool> ExistsByCodeAsync(Guid tenantId, string code, ExpenseTypeId? excludingId, CancellationToken cancellationToken);

    Task<bool> ExistsByNameAsync(Guid tenantId, string name, ExpenseTypeId? excludingId, CancellationToken cancellationToken);

    Task<PagedResult<ExpenseType>> SearchAsync(
        Guid tenantId,
        string? search,
        bool? isActive,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    /// <summary>Unpaginated — the Expense create/edit form's type picker needs every active type, not
    /// one page of them (mirrors <c>IFlatRepository.GetAllForBuildingAsync</c>'s rationale).</summary>
    Task<List<ExpenseType>> GetAllActiveForTenantAsync(Guid tenantId, CancellationToken cancellationToken);

    void Add(ExpenseType expenseType);
}
