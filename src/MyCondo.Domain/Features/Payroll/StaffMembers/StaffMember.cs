using MyCondo.Domain.Common;
using MyCondo.Domain.Common.PhoneNumbers;

namespace MyCondo.Domain.Features.Payroll.StaffMembers;

/// <summary>
/// A tenant's own employed staff member (guard, cleaner, technician, etc.) — the linkage
/// <see cref="Features.Payroll.AttendanceRecords.AttendanceRecord"/> uses for shift/attendance
/// tracking. Deliberately minimal: no salary, payroll run, or payslip fields — kickoff.md scopes the
/// full Payroll module (SalaryDisbursement) as separate, unbuilt, later-wave work; this is only the
/// master record attendance needs to exist against, per the register digitization spec's own "do not
/// calculate payroll unless an existing payroll domain exists" instruction.
/// </summary>
public sealed class StaffMember : AggregateRoot<StaffMemberId>, IAuditable, ISoftDeletable, ITenantScoped
{
    public Guid TenantId { get; private set; }
    public string FullName { get; private set; }
    public StaffRole Role { get; private set; }
    public string? Phone { get; private set; }
    public bool IsActive { get; private set; }
    public int Version { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public Guid? UpdatedBy { get; set; }

    public DateTimeOffset? DeletedAtUtc { get; set; }
    public Guid? DeletedBy { get; set; }

    private StaffMember()
    {
        FullName = null!;
    }

    private StaffMember(
        StaffMemberId id, Guid tenantId, string fullName, StaffRole role, string? phone, DateTimeOffset nowUtc)
        : base(id)
    {
        TenantId = tenantId;
        FullName = fullName;
        Role = role;
        Phone = phone;
        IsActive = true;
        Version = 1;
        CreatedAtUtc = nowUtc;
    }

    public static StaffMember Register(Guid tenantId, string fullName, StaffRole role, string? phone, DateTimeOffset nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fullName);
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        }

        return new StaffMember(
            StaffMemberId.New(), tenantId, fullName.Trim(), role, BangladeshMobileNumber.Normalize(phone), nowUtc);
    }

    public void Deactivate(DateTimeOffset nowUtc, Guid? deactivatedBy)
    {
        if (DeletedAtUtc is not null)
        {
            return;
        }

        IsActive = false;
        DeletedAtUtc = nowUtc;
        DeletedBy = deactivatedBy;
    }
}
