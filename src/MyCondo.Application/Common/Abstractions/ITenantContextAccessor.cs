namespace MyCondo.Application.Common.Abstractions;

/// <summary>
/// Resolves the current tenant for a unit of work. The Infrastructure DbContext uses this
/// to set <c>app.current_tenant_id</c> on every connection so PostgreSQL Row-Level Security
/// enforces tenant isolation at the database level.
/// </summary>
public interface ITenantContextAccessor
{
    Guid? CurrentTenantId { get; }
}
