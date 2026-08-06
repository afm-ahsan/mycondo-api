using FluentValidation;
using MyCondo.Domain.Features.Utilities.Readings;

namespace MyCondo.Application.Features.Utilities.Queries.GetReadings;

public sealed class GetReadingsQueryValidator : AbstractValidator<GetReadingsQuery>
{
    public GetReadingsQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.Status).Must(BeAValidStatus!).When(x => x.Status is not null)
            .WithMessage($"Status must be one of: {string.Join(", ", Enum.GetNames<ReadingStatus>())}.");
    }

    private static bool BeAValidStatus(string value) => Enum.TryParse<ReadingStatus>(value, out _);
}
