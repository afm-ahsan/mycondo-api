using FluentValidation;

namespace MyCondo.Application.Features.Finance.FinancialYears.Commands.CreateFinancialYear;

public sealed class CreateFinancialYearCommandValidator : AbstractValidator<CreateFinancialYearCommand>
{
    public CreateFinancialYearCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.EndDate).GreaterThan(x => x.StartDate);
    }
}
