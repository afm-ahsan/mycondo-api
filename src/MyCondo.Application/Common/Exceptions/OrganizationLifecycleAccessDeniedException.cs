using MyCondo.Domain.Features.Tenancy;

namespace MyCondo.Application.Common.Exceptions;

/// <summary>
/// The organization's own lifecycle state (<see cref="TenantStatus.Suspended"/> or
/// <see cref="TenantStatus.Closed"/>) is a full access lockout (ADR-032 §5) — distinct from
/// <see cref="ForbiddenException"/> (RBAC) and <see cref="FeatureNotEntitledException"/> (commercial
/// entitlement) so the API can map it to its own <c>tenant_suspended</c>/<c>tenant_closed</c> error codes.
/// </summary>
public sealed class OrganizationLifecycleAccessDeniedException : ApplicationException
{
    public Guid TenantId { get; }
    public TenantStatus Status { get; }

    public OrganizationLifecycleAccessDeniedException(Guid tenantId, TenantStatus status)
        : base($"Organization {tenantId} is {status} and cannot be accessed.")
    {
        TenantId = tenantId;
        Status = status;
    }
}
