using Mediator;
using MyCondo.Application.Features.Auth.DTOs;

namespace MyCondo.Application.Features.Auth.Commands.RefreshToken;

public sealed record RefreshTokenCommand(Guid TenantId, string RefreshToken) : IRequest<AuthTokensDto>;
