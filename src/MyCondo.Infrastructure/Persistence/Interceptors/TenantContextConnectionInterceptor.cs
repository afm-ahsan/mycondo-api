using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;
using MyCondo.Application.Common.Abstractions;

namespace MyCondo.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Stamps the current tenant onto every physical connection the instant it's opened — for reads and
/// writes alike, unlike the old approach of setting it only in <c>SaveChangesAsync</c> (which left
/// read paths unprotected; see mycondo-docs ADR-009). This also closes a connection-pooling leak risk:
/// because <c>ConnectionOpened</c>/<c>ConnectionOpenedAsync</c> fire on every logical open — including
/// when Npgsql hands out a pooled physical connection that a *different* tenant's request used a
/// moment ago — the GUC must be set unconditionally every time, never skipped when there's no current
/// tenant. Skipping would let a stale tenant value from a previous request leak into an anonymous or
/// different-tenant request that reuses the same pooled connection.
///
/// Sets `app.current_tenant_id` to the current tenant, or `''` (never NULL/skip) when there isn't one.
/// RLS policies must read this via `NULLIF(current_setting('app.current_tenant_id', true), '')::uuid`
/// so both "never set" and "explicitly cleared" collapse to NULL instead of `''::uuid` raising a
/// Postgres error.
/// </summary>
public sealed class TenantContextConnectionInterceptor(
    ITenantContextAccessor tenantContext
) : DbConnectionInterceptor
{
    public override async Task ConnectionOpenedAsync(
        DbConnection connection,
        ConnectionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        await SetTenantContextAsync(connection, cancellationToken);
        await base.ConnectionOpenedAsync(connection, eventData, cancellationToken);
    }

    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
        SetTenantContextAsync(connection, CancellationToken.None).GetAwaiter().GetResult();
        base.ConnectionOpened(connection, eventData);
    }

    private async Task SetTenantContextAsync(DbConnection connection, CancellationToken cancellationToken)
    {
        string tenantValue = tenantContext.CurrentTenantId?.ToString() ?? string.Empty;

        await using DbCommand command = connection.CreateCommand();
        command.CommandText = "SELECT set_config('app.current_tenant_id', @tenant_id, false);";

        DbParameter parameter = command.CreateParameter();
        parameter.ParameterName = "tenant_id";
        parameter.Value = tenantValue;
        command.Parameters.Add(parameter);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
