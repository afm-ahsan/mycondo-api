using Mediator;
using MyCondo.Application.Common.Abstractions;

namespace MyCondo.Application.Features.Platform.Commands.RevokePlatformToken;

public sealed class RevokePlatformTokenCommandHandler(
    IPlatformTokenService tokenService,
    IRequestIpAccessor ipAccessor
) : IRequestHandler<RevokePlatformTokenCommand>
{
    public async ValueTask<Unit> Handle(RevokePlatformTokenCommand command, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(command.RefreshToken))
        {
            await tokenService.RevokeAsync(command.RefreshToken, ipAccessor.IpAddress, cancellationToken);
        }
        return Unit.Value;
    }
}
