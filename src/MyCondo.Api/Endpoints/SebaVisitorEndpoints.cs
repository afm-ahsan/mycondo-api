using Mediator;
using MyCondo.Api.Authorization;
using MyCondo.Application.Features.Security.SebaVisits.Commands.CheckInSebaVisitor;
using MyCondo.Application.Features.Security.SebaVisits.Commands.CheckOutSebaVisitor;
using MyCondo.Application.Features.Security.SebaVisits.DTOs;

namespace MyCondo.Api.Endpoints;

public static class SebaVisitorEndpoints
{
    public static IEndpointRouteBuilder MapSebaVisitorEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder sebaVisits = app.MapGroup("/api/v1/seba-visits").WithTags("SebaVisitors");

        sebaVisits.MapPost("/checkin", async (CheckInSebaVisitorCommand command, ISender sender, CancellationToken ct) =>
            {
                SebaVisitDto result = await sender.Send(command, ct);
                return Results.Ok(result);
            })
            .RequirePermission("sebavisitor.manage")
            .Produces<SebaVisitDto>(StatusCodes.Status200OK);

        sebaVisits.MapPost("/{id:guid}/checkout", async (Guid id, CheckOutSebaVisitorRequest body, ISender sender, CancellationToken ct) =>
            {
                SebaVisitDto result = await sender.Send(
                    new CheckOutSebaVisitorCommand(id, body.ExitGateId, body.ServiceOutcome, body.Acknowledged), ct);
                return Results.Ok(result);
            })
            .RequirePermission("sebavisitor.checkout")
            .Produces<SebaVisitDto>(StatusCodes.Status200OK);

        return app;
    }
}

public sealed record CheckOutSebaVisitorRequest(Guid ExitGateId, string? ServiceOutcome, bool Acknowledged);
