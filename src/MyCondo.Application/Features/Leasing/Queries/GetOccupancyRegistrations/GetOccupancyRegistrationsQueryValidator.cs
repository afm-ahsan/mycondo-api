using FluentValidation;

namespace MyCondo.Application.Features.Leasing.Queries.GetOccupancyRegistrations;

public sealed class GetOccupancyRegistrationsQueryValidator : AbstractValidator<GetOccupancyRegistrationsQuery>
{
    public GetOccupancyRegistrationsQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
