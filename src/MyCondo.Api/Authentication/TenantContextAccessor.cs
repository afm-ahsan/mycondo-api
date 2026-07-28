using MyCondo.Application.Common.Abstractions;

namespace MyCondo.Api.Authentication;

/// <summary>
/// Resolves the current tenant for RLS purposes. Authenticated requests get it from the JWT
/// `tenant_id` claim. Anonymous, tenant-targeted requests (Login/Register/RefreshToken — there's no
/// JWT yet) have no claim to read, so they fall back to a value the endpoint itself stashes in
/// <see cref="HttpContext.Items"/> under <see cref="RequestedTenantItemKey"/>, sourced from the
/// `TenantId` already present in that request's own body. This is not a general "impersonate any
/// tenant" mechanism — it only ever reflects what the caller's own request already declared, and
/// RLS's `WITH CHECK` still independently rejects any write whose row doesn't match. See
/// mycondo-docs ADR-013 for the bug this fixes (Login/Register/RefreshToken silently failed once RLS
/// was enabled, because their pre-auth requests had no other way to establish tenant context).
/// </summary>
public sealed class TenantContextAccessor(
    ICurrentUserProvider currentUser,
    IHttpContextAccessor http
) : ITenantContextAccessor
{
    public const string RequestedTenantItemKey = "MyCondo.RequestedTenantId";

    public Guid? CurrentTenantId =>
        currentUser.TenantId ?? GetRequestedTenantId();

    private Guid? GetRequestedTenantId()
    {
        object? value = http.HttpContext?.Items.TryGetValue(RequestedTenantItemKey, out object? item) == true
            ? item
            : null;

        return value as Guid?;
    }
}
