using FluentValidation;
using MyCondo.Domain.Features.Amenities.Facilities;

namespace MyCondo.Application.Features.Amenities.Queries.GetFacilities;

public sealed class GetFacilitiesQueryValidator : AbstractValidator<GetFacilitiesQuery>
{
    public GetFacilitiesQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.FacilityType).Must(BeAValidFacilityType!).When(x => x.FacilityType is not null)
            .WithMessage($"FacilityType must be one of: {string.Join(", ", Enum.GetNames<FacilityType>())}.");
    }

    private static bool BeAValidFacilityType(string value) => Enum.TryParse<FacilityType>(value, out _);
}
