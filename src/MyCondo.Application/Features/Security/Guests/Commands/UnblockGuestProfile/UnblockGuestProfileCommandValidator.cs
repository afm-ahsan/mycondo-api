using FluentValidation;

namespace MyCondo.Application.Features.Security.Guests.Commands.UnblockGuestProfile;

public sealed class UnblockGuestProfileCommandValidator : AbstractValidator<UnblockGuestProfileCommand>
{
    public UnblockGuestProfileCommandValidator()
    {
        RuleFor(x => x.GuestProfileId).NotEmpty();
    }
}
