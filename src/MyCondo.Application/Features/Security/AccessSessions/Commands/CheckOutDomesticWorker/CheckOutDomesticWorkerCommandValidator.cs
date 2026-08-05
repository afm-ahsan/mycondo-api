using FluentValidation;

namespace MyCondo.Application.Features.Security.AccessSessions.Commands.CheckOutDomesticWorker;

public sealed class CheckOutDomesticWorkerCommandValidator : AbstractValidator<CheckOutDomesticWorkerCommand>
{
    public CheckOutDomesticWorkerCommandValidator()
    {
        RuleFor(x => x.AccessSessionId).NotEmpty();
        RuleFor(x => x.ExitGateId).NotEmpty();
    }
}
