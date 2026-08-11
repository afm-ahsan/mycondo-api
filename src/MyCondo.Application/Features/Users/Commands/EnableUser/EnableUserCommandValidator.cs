using FluentValidation;

namespace MyCondo.Application.Features.Users.Commands.EnableUser;

public sealed class EnableUserCommandValidator : AbstractValidator<EnableUserCommand>
{
    public EnableUserCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
    }
}
