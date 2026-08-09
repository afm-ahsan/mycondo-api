using FluentValidation;

namespace MyCondo.Application.Features.Platform.Commands.RefreshPlatformToken;

public sealed class RefreshPlatformTokenCommandValidator : AbstractValidator<RefreshPlatformTokenCommand>
{
    public RefreshPlatformTokenCommandValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty().MaximumLength(512);
    }
}
