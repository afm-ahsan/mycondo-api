using FluentValidation;

namespace MyCondo.Application.Features.Auth.Commands.Register;

public sealed class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserCommandValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(320);
        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(12).WithMessage("Password must be at least 12 characters.")
            .MaximumLength(128)
            .Matches(@"[A-Z]").WithMessage("Password must contain an uppercase letter.")
            .Matches(@"[a-z]").WithMessage("Password must contain a lowercase letter.")
            .Matches(@"\d").WithMessage("Password must contain a digit.");
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.PhoneNumber).MaximumLength(40);
    }
}
