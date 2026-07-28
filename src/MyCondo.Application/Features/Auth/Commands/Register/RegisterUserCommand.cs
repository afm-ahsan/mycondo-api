using Mediator;
using MyCondo.Application.Features.Auth.DTOs;

namespace MyCondo.Application.Features.Auth.Commands.Register;

public sealed record RegisterUserCommand(
    Guid TenantId,
    string Email,
    string Password,
    string FullName,
    string? PhoneNumber
) : IRequest<AuthTokensDto>;
