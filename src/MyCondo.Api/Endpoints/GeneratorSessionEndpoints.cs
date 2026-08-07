using Mediator;
using MyCondo.Api.Authorization;
using MyCondo.Application.Features.Operations.Commands.StartGeneratorSession;
using MyCondo.Application.Features.Operations.Commands.StopGeneratorSession;
using MyCondo.Application.Features.Operations.DTOs;
using MyCondo.Application.Features.Operations.Queries.GetGeneratorSessions;
using MyCondo.Application.Features.Operations.Queries.GetOpenGeneratorSession;
using MyCondo.Domain.Common;

namespace MyCondo.Api.Endpoints;

public static class GeneratorSessionEndpoints
{
    public static IEndpointRouteBuilder MapGeneratorSessionEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder sessions = app.MapGroup("/api/v1/generator-sessions").WithTags("Generator Sessions");

        sessions.MapPost("/", async (StartGeneratorSessionCommand command, ISender sender, CancellationToken ct) =>
            {
                GeneratorSessionDto result = await sender.Send(command, ct);
                return Results.Ok(result);
            })
            .RequirePermission("generator.operation.manage")
            .Produces<GeneratorSessionDto>(StatusCodes.Status200OK);

        sessions.MapPost("/{id:guid}/stop", async (Guid id, StopGeneratorSessionRequest body, ISender sender, CancellationToken ct) =>
            {
                GeneratorSessionDto result = await sender.Send(
                    new StopGeneratorSessionCommand(id, body.ClosingFuelLevel, body.OutageReason, body.HourMeterReading), ct);
                return Results.Ok(result);
            })
            .RequirePermission("generator.operation.manage")
            .Produces<GeneratorSessionDto>(StatusCodes.Status200OK);

        sessions.MapGet("/", async (Guid? generatorId, string? status, int page, int pageSize, ISender sender, CancellationToken ct) =>
            {
                PagedResult<GeneratorSessionDto> result = await sender.Send(
                    new GetGeneratorSessionsQuery(generatorId, status, page < 1 ? 1 : page, pageSize < 1 ? 20 : pageSize), ct);
                return Results.Ok(result);
            })
            .RequirePermission("generator.view")
            .Produces<PagedResult<GeneratorSessionDto>>(StatusCodes.Status200OK);

        sessions.MapGet("/open", async (Guid generatorId, ISender sender, CancellationToken ct) =>
            {
                GeneratorSessionDto? result = await sender.Send(new GetOpenGeneratorSessionQuery(generatorId), ct);
                return result is null ? Results.NoContent() : Results.Ok(result);
            })
            .RequirePermission("generator.view")
            .Produces<GeneratorSessionDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status204NoContent);

        return app;
    }
}

public sealed record StopGeneratorSessionRequest(decimal ClosingFuelLevel, string? OutageReason, decimal? HourMeterReading);
