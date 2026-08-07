using FluentValidation;

namespace MyCondo.Application.Features.Leasing.Queries.GetOccupancySecurityViews;

public sealed class GetOccupancySecurityViewsQueryValidator : AbstractValidator<GetOccupancySecurityViewsQuery>
{
    public GetOccupancySecurityViewsQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
