using MyCondo.Application.Common.Abstractions;

namespace MyCondo.DbMigrator;

/// <summary>
/// There is no per-request scope here (unlike the Api's HttpContext-backed accessor), so this tool
/// sets <see cref="CurrentTenantId"/> once it knows which tenant it's writing to, and
/// <c>TenantContextConnectionInterceptor</c> picks it up the same way it would for any request —
/// see ADR-009/013.
/// </summary>
public sealed class AmbientTenantContextAccessor : ITenantContextAccessor
{
    public Guid? CurrentTenantId { get; set; }
}
