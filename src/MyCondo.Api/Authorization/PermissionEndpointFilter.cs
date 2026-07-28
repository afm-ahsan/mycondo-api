using MyCondo.Application.Common.Abstractions;

namespace MyCondo.Api.Authorization;

/// <summary>
/// Denies access with 403 unless the authenticated caller holds the required permission. Assumes
/// authentication has already been enforced upstream (via <c>RequireAuthorization()</c> in
/// <see cref="EndpointRequirePermissionExtensions.RequirePermission"/>) — unauthenticated requests
/// never reach this filter.
///
/// Permission checks currently read the `perm` claims embedded in the JWT at login/refresh time
/// (see mycondo-docs/02-architecture/Architecture_Decision_Register.md ADR-011) rather than a
/// per-request server-side/Redis lookup — that's a deliberate, documented scope decision for this
/// slice, not an oversight.
/// </summary>
public sealed class PermissionEndpointFilter(string permission) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        ICurrentUserProvider currentUser =
            context.HttpContext.RequestServices.GetRequiredService<ICurrentUserProvider>();

        if (!currentUser.HasPermission(permission))
        {
            return Results.Forbid();
        }

        return await next(context);
    }
}
