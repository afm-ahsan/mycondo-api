using FluentValidation;

namespace MyCondo.Application.Features.Security.AccessSessions.Commands.CheckInServiceProvider;

public sealed class CheckInServiceProviderCommandValidator : AbstractValidator<CheckInServiceProviderCommand>
{
    public CheckInServiceProviderCommandValidator()
    {
        RuleFor(x => x.ServiceProviderProfileId).NotEmpty();
        RuleFor(x => x.HostFlatId).NotEmpty();
        RuleFor(x => x.EntryGateId).NotEmpty();
        RuleFor(x => x.Remarks).MaximumLength(500);
        RuleFor(x => x.OverrideReason).MaximumLength(400);
    }
}
