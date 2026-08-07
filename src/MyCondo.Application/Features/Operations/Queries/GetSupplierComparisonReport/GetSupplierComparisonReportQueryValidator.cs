using FluentValidation;

namespace MyCondo.Application.Features.Operations.Queries.GetSupplierComparisonReport;

public sealed class GetSupplierComparisonReportQueryValidator : AbstractValidator<GetSupplierComparisonReportQuery>
{
    public GetSupplierComparisonReportQueryValidator()
    {
        RuleFor(x => x.ToDate).GreaterThanOrEqualTo(x => x.FromDate);
    }
}
