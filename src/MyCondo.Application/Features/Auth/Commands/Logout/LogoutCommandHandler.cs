using Mediator;
using MyCondo.Application.Common.Abstractions;

namespace MyCondo.Application.Features.Auth.Commands.Logout;

public sealed class LogoutCommandHandler(
    ITokenService tokenService,
    IRequestIpAccessor ipAccessor
) : IRequestHandler<LogoutCommand>
{
    public async ValueTask<Unit> Handle(LogoutCommand command, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(command.RefreshToken))
        {
            await tokenService.RevokeAsync(command.RefreshToken, ipAccessor.IpAddress, cancellationToken);
        }
        return Unit.Value;
    }
}
