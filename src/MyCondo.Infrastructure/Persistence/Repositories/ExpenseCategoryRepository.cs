using Microsoft.EntityFrameworkCore;
using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Expenses.ExpenseCategories;

namespace MyCondo.Infrastructure.Persistence.Repositories;

public sealed class ExpenseCategoryRepository(MyCondoDbContext db) : IExpenseCategoryRepository
{
    public Task<ExpenseCategory?> GetByIdAsync(ExpenseCategoryId id, CancellationToken cancellationToken) =>
        db.Set<ExpenseCategory>().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<bool> ExistsByCodeAsync(
        Guid tenantId, string code, ExpenseCategoryId? excludingId, CancellationToken cancellationToken) =>
        db.Set<ExpenseCategory>().AnyAsync(
            x => x.TenantId == tenantId && x.Code == code && (excludingId == null || x.Id != excludingId.Value),
            cancellationToken);

    public Task<bool> ExistsByNameAsync(
        Guid tenantId, string name, ExpenseCategoryId? excludingId, CancellationToken cancellationToken) =>
        db.Set<ExpenseCategory>().AnyAsync(
            x => x.TenantId == tenantId && x.Name == name && (excludingId == null || x.Id != excludingId.Value),
            cancellationToken);

    public async Task<PagedResult<ExpenseCategory>> SearchAsync(
        Guid tenantId,
        string? search,
        bool? isActive,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        IQueryable<ExpenseCategory> query = db.Set<ExpenseCategory>()
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId);

        if (isActive is not null)
        {
            query = query.Where(x => x.IsActive == isActive);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(x =>
                EF.Functions.ILike(x.Name, $"%{search}%") || EF.Functions.ILike(x.Code, $"%{search}%"));
        }

        long total = await query.LongCountAsync(cancellationToken);

        List<ExpenseCategory> items = await query
            .OrderBy(x => x.DisplayOrder).ThenBy(x => x.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<ExpenseCategory>(items, page, pageSize, total);
    }

    public Task<List<ExpenseCategory>> GetAllActiveForTenantAsync(Guid tenantId, CancellationToken cancellationToken) =>
        db.Set<ExpenseCategory>()
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.IsActive)
            .OrderBy(x => x.DisplayOrder).ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);

    public Task<List<ExpenseCategory>> GetAllForTenantAsync(Guid tenantId, CancellationToken cancellationToken) =>
        db.Set<ExpenseCategory>()
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId)
            .OrderBy(x => x.DisplayOrder).ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);

    public void Add(ExpenseCategory expenseCategory) => db.Set<ExpenseCategory>().Add(expenseCategory);
}
