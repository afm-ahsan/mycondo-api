using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Platform.DTOs;

namespace MyCondo.Application.Features.Platform.Commands.RefreshPlatformToken;

public sealed class RefreshPlatformTokenCommandHandler(
    IPlatformTokenService tokenService,
    IRequestIpAccessor ipAccessor,
    ILogger<RefreshPlatformTokenCommandHandler> logger
) : IRequestHandler<RefreshPlatformTokenCommand, PlatformAuthTokensDto>
{
    public async ValueTask<PlatformAuthTokensDto> Handle(
        RefreshPlatformTokenCommand command, CancellationToken cancellationToken)
    {
        PlatformAuthTokensDto? tokens = await tokenService.RotateAsync(
            command.RefreshToken, ipAccessor.IpAddress, cancellationToken);

        if (tokens is null)
        {
            logger.LogInformation("Platform refresh-token rotation rejected (invalid/expired/revoked)");
            throw new ForbiddenException("Invalid or expired refresh token.");
        }

        return tokens;
    }
}
