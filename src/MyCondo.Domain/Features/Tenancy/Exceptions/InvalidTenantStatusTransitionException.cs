using MyCondo.Domain.Exceptions;

namespace MyCondo.Domain.Features.Tenancy.Exceptions;

public sealed class InvalidTenantStatusTransitionException(TenantId tenantId, TenantStatus from, TenantStatus to)
    : DomainException($"Tenant {tenantId} cannot transition from {from} to {to}.")
{
    public TenantId TenantId { get; } = tenantId;
    public TenantStatus From { get; } = from;
    public TenantStatus To { get; } = to;
}
