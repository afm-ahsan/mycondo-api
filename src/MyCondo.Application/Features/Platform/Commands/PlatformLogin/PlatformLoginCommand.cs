using Mediator;
using MyCondo.Application.Features.Platform.DTOs;

namespace MyCondo.Application.Features.Platform.Commands.PlatformLogin;

/// <summary>
/// Deliberately carries no TenantId/Organization/OrganizationCode/TenantSlug field — not optional,
/// structurally absent. A Platform SuperAdmin authenticates without ever declaring a tenant. See
/// mycondo-docs ADR-019 and the approved Phase 1 blueprint, §5 ("Platform Authentication").
/// </summary>
public sealed record PlatformLoginCommand(
    string Email,
    string Password
) : IRequest<PlatformAuthTokensDto>;
