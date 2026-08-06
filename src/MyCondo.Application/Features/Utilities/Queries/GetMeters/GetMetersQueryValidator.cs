using FluentValidation;
using MyCondo.Domain.Features.Utilities.Common;

namespace MyCondo.Application.Features.Utilities.Queries.GetMeters;

public sealed class GetMetersQueryValidator : AbstractValidator<GetMetersQuery>
{
    public GetMetersQueryValidator()
    {
        RuleFor(x => x.BuildingId).NotEmpty();
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.UtilityType).Must(BeAValidUtilityType!).When(x => x.UtilityType is not null)
            .WithMessage($"UtilityType must be one of: {string.Join(", ", Enum.GetNames<UtilityType>())}.");
    }

    private static bool BeAValidUtilityType(string value) => Enum.TryParse<UtilityType>(value, out _);
}
