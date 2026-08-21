using FluentValidation;

namespace MyCondo.Application.Features.Finance.Reports.Queries.GetFixedDepositInterestReport;

public sealed class GetFixedDepositInterestReportQueryValidator : AbstractValidator<GetFixedDepositInterestReportQuery>
{
    public GetFixedDepositInterestReportQueryValidator()
    {
        RuleFor(x => x.ToDate).GreaterThanOrEqualTo(x => x.FromDate).WithMessage("ToDate must not be before FromDate.");
    }
}
