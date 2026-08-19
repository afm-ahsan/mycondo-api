using Microsoft.EntityFrameworkCore;
using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Expenses.Expenses;
using MyCondo.Domain.Features.Expenses.ExpenseTypes;
using MyCondo.Domain.Features.Finance.Funds;
using MyCondo.Domain.Features.Property.Buildings;

namespace MyCondo.Infrastructure.Persistence.Repositories;

public sealed class ExpenseRepository(MyCondoDbContext db) : IExpenseRepository
{
    public Task<Expense?> GetByIdAsync(ExpenseId id, CancellationToken cancellationToken) =>
        db.Set<Expense>().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<bool> ExistsForExpenseTypeAsync(
        Guid tenantId, ExpenseTypeId expenseTypeId, CancellationToken cancellationToken) =>
        db.Set<Expense>().AnyAsync(
            x => x.TenantId == tenantId && x.ExpenseTypeId == expenseTypeId, cancellationToken);

    public async Task<PagedResult<Expense>> SearchAsync(
        Guid tenantId,
        BuildingId? buildingId,
        ExpenseTypeId? expenseTypeId,
        FundId? fundId,
        ExpenseStatus? status,
        DateOnly? fromDate,
        DateOnly? toDate,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        IQueryable<Expense> query = db.Set<Expense>()
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId);

        if (buildingId is not null)
        {
            query = query.Where(x => x.BuildingId == buildingId);
        }

        if (expenseTypeId is not null)
        {
            query = query.Where(x => x.ExpenseTypeId == expenseTypeId);
        }

        if (fundId is not null)
        {
            query = query.Where(x => x.FundId == fundId);
        }

        if (status is not null)
        {
            query = query.Where(x => x.Status == status);
        }

        if (fromDate is not null)
        {
            query = query.Where(x => x.ExpenseDate >= fromDate);
        }

        if (toDate is not null)
        {
            query = query.Where(x => x.ExpenseDate <= toDate);
        }

        long total = await query.LongCountAsync(cancellationToken);

        List<Expense> items = await query
            .OrderByDescending(x => x.ExpenseDate).ThenByDescending(x => x.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Expense>(items, page, pageSize, total);
    }

    public void Add(Expense expense) => db.Set<Expense>().Add(expense);
}
