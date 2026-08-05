using FluentValidation;

namespace MyCondo.Application.Features.Security.Guests.Commands.BlockGuestProfile;

public sealed class BlockGuestProfileCommandValidator : AbstractValidator<BlockGuestProfileCommand>
{
    public BlockGuestProfileCommandValidator()
    {
        RuleFor(x => x.GuestProfileId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(400);
    }
}
