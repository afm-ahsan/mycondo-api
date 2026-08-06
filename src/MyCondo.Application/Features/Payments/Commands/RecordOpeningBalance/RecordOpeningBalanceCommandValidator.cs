using FluentValidation;

namespace MyCondo.Application.Features.Payments.Commands.RecordOpeningBalance;

public sealed class RecordOpeningBalanceCommandValidator : AbstractValidator<RecordOpeningBalanceCommand>
{
    public RecordOpeningBalanceCommandValidator()
    {
        RuleFor(x => x.FlatId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.BusinessDate).NotEmpty();
        RuleFor(x => x.Description).MaximumLength(500);
    }
}
