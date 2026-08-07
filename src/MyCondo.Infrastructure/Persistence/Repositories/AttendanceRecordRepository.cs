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

    public async Task<PagedResult<AttendanceRegisterEntry>> SearchForTenantAsync(
        Guid tenantId,
        DateOnly? workDate,
        StaffMemberId? staffMemberId,
        bool? onlyOpen,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        IQueryable<AttendanceRecord> records = db.Set<AttendanceRecord>()
            .AsNoTracking()
            .Where(r => r.TenantId == tenantId);

        if (workDate is not null)
        {
            records = records.Where(r => r.WorkDate == workDate);
        }

        if (staffMemberId is not null)
        {
            records = records.Where(r => r.StaffMemberId == staffMemberId);
        }

        if (onlyOpen == true)
        {
            records = records.Where(r => r.CheckOutUtc == null);
        }

        // A single joined query, not a per-row lookup — the entire reason this method exists is to
        // avoid the N+1 shape the UX-2 discovery report flagged as the alternative. Ordered before the
        // final projection so EF Core translates it against the underlying columns directly.
        IQueryable<(AttendanceRecord Record, string FullName, string Role)> joined =
            from record in records
            join staffMember in db.Set<StaffMember>().AsNoTracking()
                on record.StaffMemberId equals staffMember.Id
            orderby record.CheckInUtc descending
            select new ValueTuple<AttendanceRecord, string, string>(record, staffMember.FullName, staffMember.Role.ToString());

        long total = await joined.LongCountAsync(cancellationToken);

        List<(AttendanceRecord Record, string FullName, string Role)> pageRows = await joined
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        List<AttendanceRegisterEntry> items = pageRows
            .Select(row => new AttendanceRegisterEntry(row.Record, row.FullName, row.Role))
            .ToList();

        return new PagedResult<AttendanceRegisterEntry>(items, page, pageSize, total);
    }

    public void Add(AttendanceRecord record) => db.Set<AttendanceRecord>().Add(record);
}
