using FluentValidation;

namespace MyCondo.Application.Features.Finance.FinancialStatements.Queries.GetIncomeExpenditureNotes;

public sealed class GetIncomeExpenditureNotesQueryValidator : AbstractValidator<GetIncomeExpenditureNotesQuery>
{
    public GetIncomeExpenditureNotesQueryValidator()
    {
        RuleFor(x => x.EndDate).GreaterThanOrEqualTo(x => x.StartDate)
            .WithMessage("EndDate must not be before StartDate.");
    }
}
