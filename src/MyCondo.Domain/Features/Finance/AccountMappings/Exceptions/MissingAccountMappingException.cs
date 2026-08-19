using MyCondo.Domain.Exceptions;

namespace MyCondo.Domain.Features.Finance.AccountMappings.Exceptions;

/// <summary>Thrown by the Application-layer centralized posting service when a tenant has no
/// <see cref="AccountMapping"/> configured for a posting role it's about to post against — the
/// template's "missing required mapping must fail explicitly... never silently post to an 'Other'
/// account" rule (ADR-027).</summary>
public sealed class MissingAccountMappingException(Guid tenantId, string postingRole)
    : DomainException($"Tenant {tenantId} has no account mapping configured for posting role '{postingRole}'.")
{
    public Guid TenantId { get; } = tenantId;
    public string PostingRole { get; } = postingRole;
}
