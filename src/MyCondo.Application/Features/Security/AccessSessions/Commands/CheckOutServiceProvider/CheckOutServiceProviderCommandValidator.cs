using FluentValidation;

namespace MyCondo.Application.Features.Security.AccessSessions.Commands.CheckOutServiceProvider;

public sealed class CheckOutServiceProviderCommandValidator : AbstractValidator<CheckOutServiceProviderCommand>
{
    public CheckOutServiceProviderCommandValidator()
    {
        RuleFor(x => x.AccessSessionId).NotEmpty();
        RuleFor(x => x.ExitGateId).NotEmpty();
    }
}
