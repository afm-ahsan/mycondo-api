using FluentValidation;

namespace MyCondo.Application.Features.Finance.AccountingPeriods.Commands.CreateAccountingPeriod;

public sealed class CreateAccountingPeriodCommandValidator : AbstractValidator<CreateAccountingPeriodCommand>
{
    public CreateAccountingPeriodCommandValidator()
    {
        RuleFor(x => x.FinancialYearId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.EndDate).GreaterThan(x => x.StartDate);
    }
}
