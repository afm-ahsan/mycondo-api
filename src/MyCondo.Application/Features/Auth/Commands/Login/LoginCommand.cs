using Mediator;
using MyCondo.Application.Features.Auth.DTOs;

namespace MyCondo.Application.Features.Auth.Commands.Login;

public sealed record LoginCommand(
    Guid TenantId,
    string Email,
    string Password
) : IRequest<AuthTokensDto>;
