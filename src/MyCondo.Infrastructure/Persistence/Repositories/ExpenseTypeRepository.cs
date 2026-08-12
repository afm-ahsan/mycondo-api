using Microsoft.EntityFrameworkCore;
using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Expenses.ExpenseTypes;

namespace MyCondo.Infrastructure.Persistence.Repositories;

public sealed class ExpenseTypeRepository(MyCondoDbContext db) : IExpenseTypeRepository
{
    public Task<ExpenseType?> GetByIdAsync(ExpenseTypeId id, CancellationToken cancellationToken) =>
        db.Set<ExpenseType>().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<bool> ExistsByCodeAsync(
        Guid tenantId, string code, ExpenseTypeId? excludingId, CancellationToken cancellationToken) =>
        db.Set<ExpenseType>().AnyAsync(
            x => x.TenantId == tenantId && x.Code == code && (excludingId == null || x.Id != excludingId.Value),
            cancellationToken);

    public Task<bool> ExistsByNameAsync(
        Guid tenantId, string name, ExpenseTypeId? excludingId, CancellationToken cancellationToken) =>
        db.Set<ExpenseType>().AnyAsync(
            x => x.TenantId == tenantId && x.Name == name && (excludingId == null || x.Id != excludingId.Value),
            cancellationToken);

    public async Task<PagedResult<ExpenseType>> SearchAsync(
        Guid tenantId,
        string? search,
        bool? isActive,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        IQueryable<ExpenseType> query = db.Set<ExpenseType>()
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

        List<ExpenseType> items = await query
            .OrderBy(x => x.DisplayOrder).ThenBy(x => x.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<ExpenseType>(items, page, pageSize, total);
    }

    public Task<List<ExpenseType>> GetAllActiveForTenantAsync(Guid tenantId, CancellationToken cancellationToken) =>
        db.Set<ExpenseType>()
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.IsActive)
            .OrderBy(x => x.DisplayOrder).ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);

    public Task<List<ExpenseType>> GetAllForTenantAsync(Guid tenantId, CancellationToken cancellationToken) =>
        db.Set<ExpenseType>()
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId)
            .OrderBy(x => x.DisplayOrder).ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);

    public void Add(ExpenseType expenseType) => db.Set<ExpenseType>().Add(expenseType);
}
