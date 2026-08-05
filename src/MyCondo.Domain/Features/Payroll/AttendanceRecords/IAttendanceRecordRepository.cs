using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Payroll.StaffMembers;

namespace MyCondo.Domain.Features.Payroll.AttendanceRecords;

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

    void Add(AttendanceRecord record);
}
