using FluentValidation;

namespace MyCondo.Application.Features.Payments.Commands.OpenResidentAccount;

public sealed class OpenResidentAccountCommandValidator : AbstractValidator<OpenResidentAccountCommand>
{
    public OpenResidentAccountCommandValidator()
    {
        RuleFor(x => x.FlatId).NotEmpty();
    }
}
