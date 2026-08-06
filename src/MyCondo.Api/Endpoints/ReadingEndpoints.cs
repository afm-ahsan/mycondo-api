using Mediator;
using MyCondo.Api.Authorization;
using MyCondo.Application.Features.Billing.DTOs;
using MyCondo.Application.Features.Utilities.Commands.BillReading;
using MyCondo.Application.Features.Utilities.Commands.CorrectReading;
using MyCondo.Application.Features.Utilities.Commands.FinalizeReading;
using MyCondo.Application.Features.Utilities.Commands.RecordReading;
using MyCondo.Application.Features.Utilities.Commands.ReviewReading;
using MyCondo.Application.Features.Utilities.DTOs;
using MyCondo.Application.Features.Utilities.Queries.GetReadingById;
using MyCondo.Application.Features.Utilities.Queries.GetReadings;
using MyCondo.Domain.Common;

namespace MyCondo.Api.Endpoints;

public static class ReadingEndpoints
{
    public static IEndpointRouteBuilder MapReadingEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder readings = app.MapGroup("/api/v1/readings").WithTags("Readings");

        readings.MapPost("/", async (RecordReadingCommand command, ISender sender, CancellationToken ct) =>
            {
                ReadingDto result = await sender.Send(command, ct);
                return Results.Ok(result);
            })
            .RequirePermission("utility.reading.record")
            .Produces<ReadingDto>(StatusCodes.Status200OK);

        readings.MapGet("/", async (Guid? meterId, Guid? flatId, string? status, int page, int pageSize, ISender sender, CancellationToken ct) =>
            {
                PagedResult<ReadingDto> result = await sender.Send(
                    new GetReadingsQuery(meterId, flatId, status, page < 1 ? 1 : page, pageSize < 1 ? 20 : pageSize), ct);
                return Results.Ok(result);
            })
            .RequirePermission("utility.reading.view")
            .Produces<PagedResult<ReadingDto>>(StatusCodes.Status200OK);

        readings.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                ReadingDto result = await sender.Send(new GetReadingByIdQuery(id), ct);
                return Results.Ok(result);
            })
            .RequirePermission("utility.reading.view")
            .Produces<ReadingDto>(StatusCodes.Status200OK);

        readings.MapPost("/{id:guid}/review", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                ReadingDto result = await sender.Send(new ReviewReadingCommand(id), ct);
                return Results.Ok(result);
            })
            .RequirePermission("utility.reading.record")
            .Produces<ReadingDto>(StatusCodes.Status200OK);

        readings.MapPost("/{id:guid}/finalize", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                ReadingDto result = await sender.Send(new FinalizeReadingCommand(id), ct);
                return Results.Ok(result);
            })
            .RequirePermission("utility.reading.finalize")
            .Produces<ReadingDto>(StatusCodes.Status200OK);

        readings.MapPost("/{id:guid}/bill", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                InvoiceDto result = await sender.Send(new BillReadingCommand(id), ct);
                return Results.Ok(result);
            })
            .RequirePermission("utility.reading.finalize")
            .RequireIdempotencyKey()
            .Produces<InvoiceDto>(StatusCodes.Status200OK);

        readings.MapPost("/{id:guid}/correct", async (Guid id, CorrectReadingRequest body, ISender sender, CancellationToken ct) =>
            {
                ReadingDto result = await sender.Send(
                    new CorrectReadingCommand(id, body.PreviousReading, body.PresentReading, body.ReadingDate, body.OverrideReason, body.Reason), ct);
                return Results.Ok(result);
            })
            .RequirePermission("utility.reading.correct")
            .RequireIdempotencyKey()
            .Produces<ReadingDto>(StatusCodes.Status200OK);

        return app;
    }
}

public sealed record CorrectReadingRequest(
    decimal PreviousReading, decimal PresentReading, DateOnly ReadingDate, string? OverrideReason, string Reason);
