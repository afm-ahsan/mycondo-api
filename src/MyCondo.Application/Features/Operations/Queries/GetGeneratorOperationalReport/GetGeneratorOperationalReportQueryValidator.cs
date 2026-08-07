using FluentValidation;

namespace MyCondo.Application.Features.Operations.Queries.GetGeneratorOperationalReport;

public sealed class GetGeneratorOperationalReportQueryValidator : AbstractValidator<GetGeneratorOperationalReportQuery>
{
    public GetGeneratorOperationalReportQueryValidator()
    {
        RuleFor(x => x.ToDate).GreaterThanOrEqualTo(x => x.FromDate);
    }
}
