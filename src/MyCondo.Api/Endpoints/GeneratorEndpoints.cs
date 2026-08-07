using Mediator;
using MyCondo.Api.Authorization;
using MyCondo.Application.Features.Operations.Commands.CreateGenerator;
using MyCondo.Application.Features.Operations.Commands.DeactivateGenerator;
using MyCondo.Application.Features.Operations.Commands.ReactivateGenerator;
using MyCondo.Application.Features.Operations.Commands.UpdateGenerator;
using MyCondo.Application.Features.Operations.DTOs;
using MyCondo.Application.Features.Operations.Queries.GetGeneratorById;
using MyCondo.Application.Features.Operations.Queries.GetGenerators;
using MyCondo.Domain.Common;

namespace MyCondo.Api.Endpoints;

public static class GeneratorEndpoints
{
    public static IEndpointRouteBuilder MapGeneratorEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder generators = app.MapGroup("/api/v1/generators").WithTags("Generators");

        generators.MapPost("/", async (CreateGeneratorCommand command, ISender sender, CancellationToken ct) =>
            {
                GeneratorDto result = await sender.Send(command, ct);
                return Results.Ok(result);
            })
            .RequirePermission("generator.manage")
            .Produces<GeneratorDto>(StatusCodes.Status200OK);

        generators.MapGet("/", async (Guid? buildingId, int page, int pageSize, ISender sender, CancellationToken ct) =>
            {
                PagedResult<GeneratorDto> result = await sender.Send(
                    new GetGeneratorsQuery(buildingId, page < 1 ? 1 : page, pageSize < 1 ? 20 : pageSize), ct);
                return Results.Ok(result);
            })
            .RequirePermission("generator.view")
            .Produces<PagedResult<GeneratorDto>>(StatusCodes.Status200OK);

        generators.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                GeneratorDto result = await sender.Send(new GetGeneratorByIdQuery(id), ct);
                return Results.Ok(result);
            })
            .RequirePermission("generator.view")
            .Produces<GeneratorDto>(StatusCodes.Status200OK);

        generators.MapPut("/{id:guid}", async (Guid id, UpdateGeneratorRequest body, ISender sender, CancellationToken ct) =>
            {
                GeneratorDto result = await sender.Send(
                    new UpdateGeneratorCommand(id, body.Name, body.Model, body.CapacityKva, body.Location), ct);
                return Results.Ok(result);
            })
            .RequirePermission("generator.manage")
            .Produces<GeneratorDto>(StatusCodes.Status200OK);

        generators.MapPost("/{id:guid}/deactivate", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                GeneratorDto result = await sender.Send(new DeactivateGeneratorCommand(id), ct);
                return Results.Ok(result);
            })
            .RequirePermission("generator.manage")
            .Produces<GeneratorDto>(StatusCodes.Status200OK);

        generators.MapPost("/{id:guid}/reactivate", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                GeneratorDto result = await sender.Send(new ReactivateGeneratorCommand(id), ct);
                return Results.Ok(result);
            })
            .RequirePermission("generator.manage")
            .Produces<GeneratorDto>(StatusCodes.Status200OK);

        return app;
    }
}

public sealed record UpdateGeneratorRequest(string Name, string? Model, decimal? CapacityKva, string? Location);
