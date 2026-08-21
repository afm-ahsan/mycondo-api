using FluentValidation;

namespace MyCondo.Application.Features.Finance.Funds.Commands.CreateFund;

public sealed class CreateFundCommandValidator : AbstractValidator<CreateFundCommand>
{
    public CreateFundCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(30);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(500);
    }
}
