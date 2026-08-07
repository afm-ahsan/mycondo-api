using FluentValidation;

namespace MyCondo.Application.Features.Amenities.Queries.GetPoolIncidents;

public sealed class GetPoolIncidentsQueryValidator : AbstractValidator<GetPoolIncidentsQuery>
{
    public GetPoolIncidentsQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
