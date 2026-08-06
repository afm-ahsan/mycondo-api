using Mediator;
using MyCondo.Api.Authorization;
using MyCondo.Application.Features.Billing.Commands.CreateServiceChargeRule;
using MyCondo.Application.Features.Billing.Commands.DeactivateServiceChargeRule;
using MyCondo.Application.Features.Billing.Commands.EndServiceChargeRulePeriod;
using MyCondo.Application.Features.Billing.DTOs;
using MyCondo.Application.Features.Billing.Queries.GetFlatsMissingArea;
using MyCondo.Application.Features.Billing.Queries.GetServiceChargeRules;
using MyCondo.Domain.Common;

namespace MyCondo.Api.Endpoints;

public static class ServiceChargeRuleEndpoints
{
    public static IEndpointRouteBuilder MapServiceChargeRuleEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder rules = app.MapGroup("/api/v1/service-charge-rules").WithTags("ServiceChargeRules");

        rules.MapPost("/", async (CreateServiceChargeRuleCommand command, ISender sender, CancellationToken ct) =>
            {
                ServiceChargeRuleDto result = await sender.Send(command, ct);
                return Results.Ok(result);
            })
            .RequirePermission("billing.rule.manage")
            .Produces<ServiceChargeRuleDto>(StatusCodes.Status200OK);

        rules.MapGet("/", async (Guid buildingId, string? category, int page, int pageSize, ISender sender, CancellationToken ct) =>
            {
                PagedResult<ServiceChargeRuleDto> result = await sender.Send(
                    new GetServiceChargeRulesQuery(buildingId, category, page < 1 ? 1 : page, pageSize < 1 ? 20 : pageSize), ct);
                return Results.Ok(result);
            })
            .RequirePermission("billing.rule.view")
            .Produces<PagedResult<ServiceChargeRuleDto>>(StatusCodes.Status200OK);

        rules.MapGet("/flats-missing-area", async (Guid buildingId, int page, int pageSize, ISender sender, CancellationToken ct) =>
            {
                PagedResult<FlatMissingAreaDto> result = await sender.Send(
                    new GetFlatsMissingAreaQuery(buildingId, page < 1 ? 1 : page, pageSize < 1 ? 20 : pageSize), ct);
                return Results.Ok(result);
            })
            .RequirePermission("billing.rule.view")
            .Produces<PagedResult<FlatMissingAreaDto>>(StatusCodes.Status200OK);

        rules.MapPost("/{id:guid}/deactivate", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                ServiceChargeRuleDto result = await sender.Send(new DeactivateServiceChargeRuleCommand(id), ct);
                return Results.Ok(result);
            })
            .RequirePermission("billing.rule.manage")
            .Produces<ServiceChargeRuleDto>(StatusCodes.Status200OK);

        rules.MapPost("/{id:guid}/end-effective-period", async (Guid id, EndServiceChargeRulePeriodRequest body, ISender sender, CancellationToken ct) =>
            {
                ServiceChargeRuleDto result = await sender.Send(new EndServiceChargeRulePeriodCommand(id, body.EffectiveTo), ct);
                return Results.Ok(result);
            })
            .RequirePermission("billing.rule.manage")
            .Produces<ServiceChargeRuleDto>(StatusCodes.Status200OK);

        return app;
    }
}

public sealed record EndServiceChargeRulePeriodRequest(DateOnly EffectiveTo);
