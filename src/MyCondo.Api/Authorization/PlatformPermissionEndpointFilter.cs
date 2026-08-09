using MyCondo.Application.Common.Abstractions;

namespace MyCondo.Api.Authorization;

/// <summary>
/// Platform-scope analogue of <see cref="PermissionEndpointFilter"/> — denies with 403 unless the
/// caller (already authenticated via the "Platform" scheme by
/// <see cref="EndpointRequirePlatformPermissionExtensions.RequirePlatformPermission"/>) holds the
/// required Platform permission.
/// </summary>
public sealed class PlatformPermissionEndpointFilter(string permission) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        ICurrentPlatformUserProvider currentUser =
            context.HttpContext.RequestServices.GetRequiredService<ICurrentPlatformUserProvider>();

        if (!currentUser.HasPermission(permission))
        {
            return Results.Forbid();
        }

        return await next(context);
    }
}
