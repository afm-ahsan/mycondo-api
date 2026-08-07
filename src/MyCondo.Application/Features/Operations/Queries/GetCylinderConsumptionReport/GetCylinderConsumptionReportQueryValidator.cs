using FluentValidation;

namespace MyCondo.Application.Features.Operations.Queries.GetCylinderConsumptionReport;

public sealed class GetCylinderConsumptionReportQueryValidator : AbstractValidator<GetCylinderConsumptionReportQuery>
{
    public GetCylinderConsumptionReportQueryValidator()
    {
        RuleFor(x => x.ToDate).GreaterThanOrEqualTo(x => x.FromDate);
    }
}
