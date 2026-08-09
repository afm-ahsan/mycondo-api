using Mediator;
using MyCondo.Application.Features.Platform.DTOs;

namespace MyCondo.Application.Features.Platform.Commands.RefreshPlatformToken;

public sealed record RefreshPlatformTokenCommand(string RefreshToken) : IRequest<PlatformAuthTokensDto>;
