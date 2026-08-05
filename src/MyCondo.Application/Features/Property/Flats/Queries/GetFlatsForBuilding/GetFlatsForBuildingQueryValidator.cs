using FluentValidation;

namespace MyCondo.Application.Features.Property.Flats.Queries.GetFlatsForBuilding;

public sealed class GetFlatsForBuildingQueryValidator : AbstractValidator<GetFlatsForBuildingQuery>
{
    public GetFlatsForBuildingQueryValidator()
    {
        RuleFor(x => x.BuildingId).NotEmpty();
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.Search).MaximumLength(200);
    }
}
