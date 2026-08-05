using FluentValidation;

namespace MyCondo.Application.Features.Security.AccessSessions.Commands.CheckInGuest;

public sealed class CheckInGuestCommandValidator : AbstractValidator<CheckInGuestCommand>
{
    public CheckInGuestCommandValidator()
    {
        RuleFor(x => x.GuestProfileId).NotEmpty();
        RuleFor(x => x.HostFlatId).NotEmpty();
        RuleFor(x => x.EntryGateId).NotEmpty();
        RuleFor(x => x.PurposeOfVisit).MaximumLength(200);
        RuleFor(x => x.PassOrQrNumber).MaximumLength(80);
        RuleFor(x => x.Remarks).MaximumLength(500);
        RuleFor(x => x.OverrideReason).MaximumLength(400);
    }
}
