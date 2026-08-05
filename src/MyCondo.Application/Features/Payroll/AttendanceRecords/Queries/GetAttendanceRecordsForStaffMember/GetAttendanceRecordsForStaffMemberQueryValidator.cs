using FluentValidation;

namespace MyCondo.Application.Features.Payroll.AttendanceRecords.Queries.GetAttendanceRecordsForStaffMember;

public sealed class GetAttendanceRecordsForStaffMemberQueryValidator : AbstractValidator<GetAttendanceRecordsForStaffMemberQuery>
{
    public GetAttendanceRecordsForStaffMemberQueryValidator()
    {
        RuleFor(x => x.StaffMemberId).NotEmpty();
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
