using FluentValidation;
using MyCondo.Domain.Features.Utilities.Common;

namespace MyCondo.Application.Features.Utilities.Queries.GetReadingStatusSummaryReport;

public sealed class GetReadingStatusSummaryReportQueryValidator : AbstractValidator<GetReadingStatusSummaryReportQuery>
{
    public GetReadingStatusSummaryReportQueryValidator()
    {
        RuleFor(x => x.UtilityType).Must(BeAValidUtilityType!).When(x => x.UtilityType is not null)
            .WithMessage($"UtilityType must be one of: {string.Join(", ", Enum.GetNames<UtilityType>())}.");
    }

    private static bool BeAValidUtilityType(string value) => Enum.TryParse<UtilityType>(value, out _);
}
