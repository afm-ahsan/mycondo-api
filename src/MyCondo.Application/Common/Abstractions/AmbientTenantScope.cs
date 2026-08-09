namespace MyCondo.Application.Common.Abstractions;

/// <summary>
/// Ambient tenant-context fallback for code paths that have neither a JWT claim nor an HTTP request to
/// stash a requested tenant on — see <c>MyCondo.Api.Authentication.TenantContextAccessor</c>, whose
/// Login/Register/RefreshToken side-channel (ADR-013) only exists because those endpoints run inside
/// an HTTP request. Startup <c>IHostedService</c> seeding (e.g. development bootstrap) runs before
/// Kestrel accepts any request, so there is no request to attach anything to.
///
/// AsyncLocal-scoped: <see cref="Begin"/> only affects the async call chain descending from where it
/// was invoked. It never leaks into unrelated request-handling flows, because those begin as new,
/// independent async flows (Kestrel's own accept loop), not children of the hosted service's call
/// stack. Mirrors the same settable-accessor idea already used for non-HTTP contexts elsewhere in this
/// codebase (<c>tools/MyCondo.DbMigrator/AmbientTenantContextAccessor</c>,
/// <c>MyCondo.MultiTenancyTests/TestTenantContextAccessor</c>).
/// </summary>
public static class AmbientTenantScope
{
    private static readonly AsyncLocal<Guid?> CurrentValue = new();

    public static Guid? Current => CurrentValue.Value;

    public static IDisposable Begin(Guid tenantId)
    {
        Guid? previous = CurrentValue.Value;
        CurrentValue.Value = tenantId;
        return new Scope(previous);
    }

    private sealed class Scope(Guid? previous) : IDisposable
    {
        public void Dispose() => CurrentValue.Value = previous;
    }
}
