using FluentValidation;

namespace MyCondo.Application.Features.Auth.Commands.ChangePassword;

public sealed class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty().MaximumLength(128);
        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .MinimumLength(12)
            .MaximumLength(128)
            .Matches(@"[A-Z]")
            .Matches(@"[a-z]")
            .Matches(@"\d");
        RuleFor(x => x.NewPassword)
            .NotEqual(x => x.CurrentPassword)
            .WithMessage("New password must differ from current.");
    }
}
