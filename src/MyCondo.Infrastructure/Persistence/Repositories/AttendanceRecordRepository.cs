using Microsoft.EntityFrameworkCore;
using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Payroll.AttendanceRecords;
using MyCondo.Domain.Features.Payroll.StaffMembers;

namespace MyCondo.Infrastructure.Persistence.Repositories;

public sealed class AttendanceRecordRepository(MyCondoDbContext db) : IAttendanceRecordRepository
{
    public Task<AttendanceRecord?> GetByIdAsync(AttendanceRecordId id, CancellationToken cancellationToken) =>
        db.Set<AttendanceRecord>().FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public Task<AttendanceRecord?> GetOpenRecordForStaffMemberAsync(
        Guid tenantId, StaffMemberId staffMemberId, CancellationToken cancellationToken) =>
        db.Set<AttendanceRecord>().FirstOrDefaultAsync(
            r => r.TenantId == tenantId && r.StaffMemberId == staffMemberId && r.CheckOutUtc == null,
            cancellationToken);

    public async Task<PagedResult<AttendanceRecord>> SearchForStaffMemberAsync(
        Guid tenantId,
        StaffMemberId staffMemberId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        IQueryable<AttendanceRecord> query = db.Set<AttendanceRecord>()
            .AsNoTracking()
            .Where(r => r.TenantId == tenantId && r.StaffMemberId == staffMemberId);

        long total = await query.LongCountAsync(cancellationToken);

        List<AttendanceRecord> items = await query
            .OrderByDescending(r => r.CheckInUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<AttendanceRecord>(items, page, pageSize, total);
    }

    public void Add(AttendanceRecord record) => db.Set<AttendanceRecord>().Add(record);
}
