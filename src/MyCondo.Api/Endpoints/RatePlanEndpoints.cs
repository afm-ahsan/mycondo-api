using Mediator;
using MyCondo.Api.Authorization;
using MyCondo.Application.Features.Utilities.Commands.CreateRatePlan;
using MyCondo.Application.Features.Utilities.Commands.DeactivateRatePlan;
using MyCondo.Application.Features.Utilities.Commands.EndRatePlanPeriod;
using MyCondo.Application.Features.Utilities.DTOs;
using MyCondo.Application.Features.Utilities.Queries.GetRatePlans;
using MyCondo.Domain.Common;

namespace MyCondo.Api.Endpoints;

public static class RatePlanEndpoints
{
    public static IEndpointRouteBuilder MapRatePlanEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder ratePlans = app.MapGroup("/api/v1/rate-plans").WithTags("RatePlans");

        ratePlans.MapPost("/", async (CreateRatePlanCommand command, ISender sender, CancellationToken ct) =>
            {
                RatePlanDto result = await sender.Send(command, ct);
                return Results.Ok(result);
            })
            .RequirePermission("utility.rateplan.manage")
            .Produces<RatePlanDto>(StatusCodes.Status200OK);

        ratePlans.MapGet("/", async (Guid buildingId, string? utilityType, int page, int pageSize, ISender sender, CancellationToken ct) =>
            {
                PagedResult<RatePlanDto> result = await sender.Send(
                    new GetRatePlansQuery(buildingId, utilityType, page < 1 ? 1 : page, pageSize < 1 ? 20 : pageSize), ct);
                return Results.Ok(result);
            })
            .RequirePermission("utility.rateplan.view")
            .Produces<PagedResult<RatePlanDto>>(StatusCodes.Status200OK);

        ratePlans.MapPost("/{id:guid}/end-effective-period", async (Guid id, EndRatePlanPeriodRequest body, ISender sender, CancellationToken ct) =>
            {
                RatePlanDto result = await sender.Send(new EndRatePlanPeriodCommand(id, body.EffectiveTo), ct);
                return Results.Ok(result);
            })
            .RequirePermission("utility.rateplan.manage")
            .Produces<RatePlanDto>(StatusCodes.Status200OK);

        ratePlans.MapPost("/{id:guid}/deactivate", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                RatePlanDto result = await sender.Send(new DeactivateRatePlanCommand(id), ct);
                return Results.Ok(result);
            })
            .RequirePermission("utility.rateplan.manage")
            .Produces<RatePlanDto>(StatusCodes.Status200OK);

        return app;
    }
}

public sealed record EndRatePlanPeriodRequest(DateOnly EffectiveTo);
