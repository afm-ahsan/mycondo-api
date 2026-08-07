using FluentValidation;

namespace MyCondo.Application.Features.Payroll.AttendanceRecords.Queries.GetAttendanceRecordsForTenant;

public sealed class GetAttendanceRecordsForTenantQueryValidator : AbstractValidator<GetAttendanceRecordsForTenantQuery>
{
    public GetAttendanceRecordsForTenantQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
