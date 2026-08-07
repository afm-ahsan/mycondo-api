using FluentValidation;

namespace MyCondo.Application.Features.Amenities.Queries.GetPoolSessions;

public sealed class GetPoolSessionsQueryValidator : AbstractValidator<GetPoolSessionsQuery>
{
    public GetPoolSessionsQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
