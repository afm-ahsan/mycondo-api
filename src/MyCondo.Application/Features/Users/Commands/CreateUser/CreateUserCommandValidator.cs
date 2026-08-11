using FluentValidation;

namespace MyCondo.Application.Features.Users.Commands.CreateUser;

public sealed class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(320);
        RuleFor(x => x.PhoneNumber).MaximumLength(40);

        When(x => x.InitialPassword is not null, () =>
        {
            RuleFor(x => x.InitialPassword!)
                .MinimumLength(12).WithMessage("Password must be at least 12 characters.")
                .MaximumLength(128)
                .Matches(@"[A-Z]").WithMessage("Password must contain an uppercase letter.")
                .Matches(@"[a-z]").WithMessage("Password must contain a lowercase letter.")
                .Matches(@"\d").WithMessage("Password must contain a digit.");
        });
    }
}
