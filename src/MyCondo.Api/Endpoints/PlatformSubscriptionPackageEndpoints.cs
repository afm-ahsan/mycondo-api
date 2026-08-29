using Mediator;
using MyCondo.Api.Authorization;
using MyCondo.Application.Features.Platform.DTOs;
using MyCondo.Application.Features.Platform.Queries.GetSubscriptionPackageOptions;

namespace MyCondo.Api.Endpoints;

/// <summary>
/// Platform-scope read-only subscription package catalogue (ADR-033 Task 13E.1). The sole consumer
/// today is the organization provisioning wizard: it needs to offer real, currently-assignable
/// package/version/billing-cycle/price choices instead of the wizard hard-coding or omitting them.
/// </summary>
public static class PlatformSubscriptionPackageEndpoints
{
    public static IEndpointRouteBuilder MapPlatformSubscriptionPackageEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/v1/platform/subscription-packages")
            .WithTags("Platform Subscription Packages");

        group.MapGet("/", async (ISender sender, CancellationToken ct) =>
            {
                List<SubscriptionPackageOptionDto> result = await sender.Send(
                    new GetSubscriptionPackageOptionsQuery(), ct);
                return Results.Ok(result);
            })
            .RequirePlatformPermission("platform.subscription.read")
            .Produces<List<SubscriptionPackageOptionDto>>(StatusCodes.Status200OK);

        return app;
    }
}
