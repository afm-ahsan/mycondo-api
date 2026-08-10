namespace MyCondo.Domain.Features.Tenancy;

public interface ITenantModuleRepository
{
    Task<List<TenantModule>> GetEnabledForTenantAsync(Guid tenantId, CancellationToken cancellationToken);

    /// <summary>Enabled-module count per tenant, for cheap list-view rendering (no per-row module
    /// key fetch).</summary>
    Task<Dictionary<Guid, int>> GetEnabledCountsAsync(IReadOnlyCollection<Guid> tenantIds, CancellationToken cancellationToken);

    /// <summary>Idempotent set-replace: enables exactly <paramref name="moduleKeys"/> for the tenant,
    /// disabling (removing) any currently-enabled module not in that set. Caller must call
    /// <c>IUnitOfWork.SaveChangesAsync</c> to persist.</summary>
    Task ReplaceForTenantAsync(
        Guid tenantId,
        IReadOnlyCollection<string> moduleKeys,
        DateTimeOffset nowUtc,
        Guid? enabledBy,
        CancellationToken cancellationToken);
}
