using FluentValidation;

namespace MyCondo.Application.Features.Finance.FinancialStatements.Queries.GetIncomeExpenditureStatement;

public sealed class GetIncomeExpenditureStatementQueryValidator : AbstractValidator<GetIncomeExpenditureStatementQuery>
{
    public GetIncomeExpenditureStatementQueryValidator()
    {
        RuleFor(x => x.EndDate).GreaterThanOrEqualTo(x => x.StartDate)
            .WithMessage("EndDate must not be before StartDate.");
    }
}
