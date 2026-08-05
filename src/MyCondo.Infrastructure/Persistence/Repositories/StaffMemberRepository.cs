using Microsoft.EntityFrameworkCore;
using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Payroll.StaffMembers;

namespace MyCondo.Infrastructure.Persistence.Repositories;

public sealed class StaffMemberRepository(MyCondoDbContext db) : IStaffMemberRepository
{
    public Task<StaffMember?> GetByIdAsync(StaffMemberId id, CancellationToken cancellationToken) =>
        db.Set<StaffMember>().FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public async Task<PagedResult<StaffMember>> SearchAsync(
        Guid tenantId,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        IQueryable<StaffMember> query = db.Set<StaffMember>()
            .AsNoTracking()
            .Where(s => s.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(s => EF.Functions.ILike(s.FullName, $"%{search}%"));
        }

        long total = await query.LongCountAsync(cancellationToken);

        List<StaffMember> items = await query
            .OrderBy(s => s.FullName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<StaffMember>(items, page, pageSize, total);
    }

    public void Add(StaffMember staffMember) => db.Set<StaffMember>().Add(staffMember);
}
