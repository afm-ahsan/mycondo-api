using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Auth.DTOs;

namespace MyCondo.Application.Features.Auth.Commands.RefreshToken;

public sealed class RefreshTokenCommandHandler(
    ITokenService tokenService,
    IRequestIpAccessor ipAccessor,
    ILogger<RefreshTokenCommandHandler> logger
) : IRequestHandler<RefreshTokenCommand, AuthTokensDto>
{
    public async ValueTask<AuthTokensDto> Handle(RefreshTokenCommand command, CancellationToken cancellationToken)
    {
        AuthTokensDto? tokens = await tokenService.RotateAsync(
            command.RefreshToken, ipAccessor.IpAddress, cancellationToken);

        if (tokens is null)
        {
            logger.LogInformation("Refresh-token rotation rejected (invalid/expired/revoked)");
            throw new ForbiddenException("Invalid or expired refresh token.");
        }

        return tokens;
    }
}
