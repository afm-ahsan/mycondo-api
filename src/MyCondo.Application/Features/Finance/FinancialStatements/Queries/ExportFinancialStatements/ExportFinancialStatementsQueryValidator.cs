using FluentValidation;

namespace MyCondo.Application.Features.Finance.FinancialStatements.Queries.ExportFinancialStatements;

public sealed class ExportFinancialStatementsQueryValidator : AbstractValidator<ExportFinancialStatementsQuery>
{
    public ExportFinancialStatementsQueryValidator()
    {
        RuleFor(x => x.EndDate).GreaterThanOrEqualTo(x => x.StartDate)
            .WithMessage("EndDate must not be before StartDate.");
    }
}
