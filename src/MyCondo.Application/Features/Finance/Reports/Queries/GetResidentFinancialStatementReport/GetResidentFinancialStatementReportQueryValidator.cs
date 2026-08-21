using FluentValidation;

namespace MyCondo.Application.Features.Finance.Reports.Queries.GetResidentFinancialStatementReport;

public sealed class GetResidentFinancialStatementReportQueryValidator : AbstractValidator<GetResidentFinancialStatementReportQuery>
{
    public GetResidentFinancialStatementReportQueryValidator()
    {
        RuleFor(x => x.FlatId).NotEmpty();
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 200);
        RuleFor(x => x.ToDate).GreaterThanOrEqualTo(x => x.FromDate!.Value)
            .When(x => x.FromDate is not null && x.ToDate is not null)
            .WithMessage("ToDate must not be before FromDate.");
    }
}
