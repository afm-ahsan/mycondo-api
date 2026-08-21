using Microsoft.EntityFrameworkCore;
using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Expenses.ExpenseCategories;
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

    public async Task<IReadOnlyList<ExpenseCategoryActivityLine>> GetExpenseCompositionByCategoryAsync(
        Guid tenantId, DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken)
    {
        var joined =
            from x in db.Set<Expense>()
            join t in db.Set<ExpenseType>() on x.ExpenseTypeId equals t.Id
            join c in db.Set<ExpenseCategory>() on t.ExpenseCategoryId equals c.Id into categories
            from c in categories.DefaultIfEmpty()
            where x.TenantId == tenantId
                && (x.Status == ExpenseStatus.Posted || x.Status == ExpenseStatus.Paid)
                && x.AccountingDate >= fromDate && x.AccountingDate <= toDate
            select new { CategoryId = (ExpenseCategoryId?)c.Id, CategoryName = c.Name, x.Amount };

        var grouped = await joined
            .GroupBy(x => new { x.CategoryId, x.CategoryName })
            .Select(g => new { g.Key.CategoryId, g.Key.CategoryName, Total = g.Sum(x => x.Amount) })
            .ToListAsync(cancellationToken);

        return grouped
            .Select(g => new ExpenseCategoryActivityLine(g.CategoryId, g.CategoryName ?? "Uncategorized", g.Total))
            .OrderByDescending(l => l.Total)
            .ToList();
    }

    public async Task<IReadOnlyList<ExpenseTypeActivityLine>> GetExpenseCompositionByTypeAsync(
        Guid tenantId, DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken)
    {
        var joined =
            from x in db.Set<Expense>()
            join t in db.Set<ExpenseType>() on x.ExpenseTypeId equals t.Id
            join c in db.Set<ExpenseCategory>() on t.ExpenseCategoryId equals c.Id into categories
            from c in categories.DefaultIfEmpty()
            where x.TenantId == tenantId
                && (x.Status == ExpenseStatus.Posted || x.Status == ExpenseStatus.Paid)
                && x.AccountingDate >= fromDate && x.AccountingDate <= toDate
            select new
            {
                TypeId = t.Id, TypeName = t.Name, CategoryId = (ExpenseCategoryId?)c.Id, CategoryName = c.Name,
                x.Amount,
            };

        var grouped = await joined
            .GroupBy(x => new { x.TypeId, x.TypeName, x.CategoryId, x.CategoryName })
            .Select(g => new
            {
                g.Key.TypeId, g.Key.TypeName, g.Key.CategoryId, g.Key.CategoryName,
                Count = g.Count(), Total = g.Sum(x => x.Amount),
            })
            .ToListAsync(cancellationToken);

        return grouped
            .Select(g => new ExpenseTypeActivityLine(
                g.TypeId, g.TypeName, g.CategoryId, g.CategoryName ?? "Uncategorized", g.Count, g.Total))
            .OrderByDescending(l => l.Total)
            .ToList();
    }

    public async Task<IReadOnlyList<ExpenseStatusTotal>> GetStatusTotalsForPeriodAsync(
        Guid tenantId, DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken)
    {
        return await db.Set<Expense>()
            .Where(x => x.TenantId == tenantId && x.AccountingDate >= fromDate && x.AccountingDate <= toDate)
            .GroupBy(x => x.Status)
            .Select(g => new ExpenseStatusTotal(g.Key, g.Count(), g.Sum(x => x.Amount)))
            .ToListAsync(cancellationToken);
    }

    public void Add(Expense expense) => db.Set<Expense>().Add(expense);
}
