using FluentValidation;
using MyCondo.Domain.Features.Utilities.Common;

namespace MyCondo.Application.Features.Utilities.Queries.GetConsumptionSummaryReport;

public sealed class GetConsumptionSummaryReportQueryValidator : AbstractValidator<GetConsumptionSummaryReportQuery>
{
    public GetConsumptionSummaryReportQueryValidator()
    {
        RuleFor(x => x.FromDate).NotEmpty();
        RuleFor(x => x.ToDate).GreaterThanOrEqualTo(x => x.FromDate)
            .WithMessage("ToDate must not precede FromDate.");
        RuleFor(x => x.UtilityType).Must(BeAValidUtilityType!).When(x => x.UtilityType is not null)
            .WithMessage($"UtilityType must be one of: {string.Join(", ", Enum.GetNames<UtilityType>())}.");
    }

    private static bool BeAValidUtilityType(string value) => Enum.TryParse<UtilityType>(value, out _);
}
