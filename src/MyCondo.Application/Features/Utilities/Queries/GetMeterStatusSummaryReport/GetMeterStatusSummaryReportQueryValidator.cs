using FluentValidation;
using MyCondo.Domain.Features.Utilities.Common;

namespace MyCondo.Application.Features.Utilities.Queries.GetMeterStatusSummaryReport;

public sealed class GetMeterStatusSummaryReportQueryValidator : AbstractValidator<GetMeterStatusSummaryReportQuery>
{
    public GetMeterStatusSummaryReportQueryValidator()
    {
        RuleFor(x => x.UtilityType).Must(BeAValidUtilityType!).When(x => x.UtilityType is not null)
            .WithMessage($"UtilityType must be one of: {string.Join(", ", Enum.GetNames<UtilityType>())}.");
    }

    private static bool BeAValidUtilityType(string value) => Enum.TryParse<UtilityType>(value, out _);
}
