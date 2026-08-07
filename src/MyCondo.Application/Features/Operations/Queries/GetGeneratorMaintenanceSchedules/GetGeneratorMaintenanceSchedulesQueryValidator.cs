using FluentValidation;

namespace MyCondo.Application.Features.Operations.Queries.GetGeneratorMaintenanceSchedules;

public sealed class GetGeneratorMaintenanceSchedulesQueryValidator : AbstractValidator<GetGeneratorMaintenanceSchedulesQuery>
{
    public GetGeneratorMaintenanceSchedulesQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
