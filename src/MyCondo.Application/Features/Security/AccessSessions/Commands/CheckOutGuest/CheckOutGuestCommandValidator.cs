using FluentValidation;

namespace MyCondo.Application.Features.Security.AccessSessions.Commands.CheckOutGuest;

public sealed class CheckOutGuestCommandValidator : AbstractValidator<CheckOutGuestCommand>
{
    public CheckOutGuestCommandValidator()
    {
        RuleFor(x => x.AccessSessionId).NotEmpty();
        RuleFor(x => x.ExitGateId).NotEmpty();
    }
}
