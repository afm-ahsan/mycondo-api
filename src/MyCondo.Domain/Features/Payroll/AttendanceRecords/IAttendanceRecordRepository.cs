using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Payroll.StaffMembers;

namespace MyCondo.Domain.Features.Payroll.AttendanceRecords;

/// <summary>
/// One row of a tenant-wide attendance register/currently-present view — a read-side projection
/// joining <see cref="AttendanceRecord"/> with its owning <see cref="StaffMember"/>'s display fields,
/// since a register is meaningless without knowing who each row belongs to. Deliberately not part of
/// the Application layer's AttendanceRecordDto (which mirrors the aggregate itself) — this shape only
/// exists for <see cref="IAttendanceRecordRepository.SearchForTenantAsync"/>.
/// </summary>
public sealed record AttendanceRegisterEntry(
    AttendanceRecord Record,
    string StaffMemberFullName,
    string StaffMemberRole);

public interface IAttendanceRecordRepository
{
    Task<AttendanceRecord?> GetByIdAsync(AttendanceRecordId id, CancellationToken cancellationToken);

    Task<AttendanceRecord?> GetOpenRecordForStaffMemberAsync(
        Guid tenantId, StaffMemberId staffMemberId, CancellationToken cancellationToken);

    Task<PagedResult<AttendanceRecord>> SearchForStaffMemberAsync(
        Guid tenantId,
        StaffMemberId staffMemberId,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    /// <summary>
    /// Tenant-wide attendance register — serves both "attendance for date X across all staff"
    /// (<paramref name="workDate"/> set) and "who is currently present" (<paramref name="onlyOpen"/>
    /// true, <paramref name="workDate"/> typically today or omitted). Added for UX-2 Staff Attendance;
    /// see the UX-2 discovery report's API Gap Analysis for the full rationale — the frontend has no
    /// other way to build a register or presence view without fetching every staff member's own
    /// history individually.
    /// </summary>
    Task<PagedResult<AttendanceRegisterEntry>> SearchForTenantAsync(
        Guid tenantId,
        DateOnly? workDate,
        StaffMemberId? staffMemberId,
        bool? onlyOpen,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    void Add(AttendanceRecord record);
}
