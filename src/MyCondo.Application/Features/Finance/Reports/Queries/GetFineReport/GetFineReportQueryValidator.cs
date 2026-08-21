using FluentValidation;

namespace MyCondo.Application.Features.Finance.Reports.Queries.GetFineReport;

public sealed class GetFineReportQueryValidator : AbstractValidator<GetFineReportQuery>
{
    public GetFineReportQueryValidator()
    {
        RuleFor(x => x.ToDate).GreaterThanOrEqualTo(x => x.FromDate)
            .WithMessage("ToDate must not be before FromDate.");
    }
}
