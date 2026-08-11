using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Expenses.ExpenseTypes;
using MyCondo.Domain.Features.Property.Buildings;

namespace MyCondo.Domain.Features.Expenses.Expenses;

public interface IExpenseRepository
{
    Task<Expense?> GetByIdAsync(ExpenseId id, CancellationToken cancellationToken);

    Task<bool> ExistsForExpenseTypeAsync(Guid tenantId, ExpenseTypeId expenseTypeId, CancellationToken cancellationToken);

    Task<PagedResult<Expense>> SearchAsync(
        Guid tenantId,
        BuildingId? buildingId,
        ExpenseTypeId? expenseTypeId,
        ExpenseStatus? status,
        DateOnly? fromDate,
        DateOnly? toDate,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    void Add(Expense expense);
}
