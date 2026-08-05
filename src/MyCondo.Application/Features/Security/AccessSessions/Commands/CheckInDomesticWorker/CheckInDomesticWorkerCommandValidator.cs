using FluentValidation;

namespace MyCondo.Application.Features.Security.AccessSessions.Commands.CheckInDomesticWorker;

public sealed class CheckInDomesticWorkerCommandValidator : AbstractValidator<CheckInDomesticWorkerCommand>
{
    public CheckInDomesticWorkerCommandValidator()
    {
        RuleFor(x => x.DomesticWorkerProfileId).NotEmpty();
        RuleFor(x => x.HostFlatId).NotEmpty();
        RuleFor(x => x.EntryGateId).NotEmpty();
        RuleFor(x => x.Remarks).MaximumLength(500);
        RuleFor(x => x.OverrideReason).MaximumLength(400);
    }
}
