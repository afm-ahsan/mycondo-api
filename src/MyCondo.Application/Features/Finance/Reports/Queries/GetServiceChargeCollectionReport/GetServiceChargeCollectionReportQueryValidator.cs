using FluentValidation;

namespace MyCondo.Application.Features.Finance.Reports.Queries.GetServiceChargeCollectionReport;

public sealed class GetServiceChargeCollectionReportQueryValidator : AbstractValidator<GetServiceChargeCollectionReportQuery>
{
    public GetServiceChargeCollectionReportQueryValidator()
    {
        RuleFor(x => x.ToDate).GreaterThanOrEqualTo(x => x.FromDate)
            .WithMessage("ToDate must not be before FromDate.");
    }
}
