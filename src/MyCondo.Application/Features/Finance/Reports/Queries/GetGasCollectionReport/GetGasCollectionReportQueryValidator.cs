using FluentValidation;

namespace MyCondo.Application.Features.Finance.Reports.Queries.GetGasCollectionReport;

public sealed class GetGasCollectionReportQueryValidator : AbstractValidator<GetGasCollectionReportQuery>
{
    public GetGasCollectionReportQueryValidator()
    {
        RuleFor(x => x.ToDate).GreaterThanOrEqualTo(x => x.FromDate)
            .WithMessage("ToDate must not be before FromDate.");
    }
}
