using Mediator;
using MyCondo.Api.Authorization;
using MyCondo.Application.Features.Operations.Commands.RecordFuelReceipt;
using MyCondo.Application.Features.Operations.DTOs;
using MyCondo.Application.Features.Operations.Queries.GetGeneratorFuelReceipts;
using MyCondo.Domain.Common;

namespace MyCondo.Api.Endpoints;

public static class GeneratorFuelReceiptEndpoints
{
    public static IEndpointRouteBuilder MapGeneratorFuelReceiptEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder receipts = app.MapGroup("/api/v1/generator-fuel-receipts").WithTags("Generator Fuel Receipts");

        receipts.MapPost("/", async (RecordFuelReceiptCommand command, ISender sender, CancellationToken ct) =>
            {
                GeneratorFuelReceiptDto result = await sender.Send(command, ct);
                return Results.Ok(result);
            })
            .RequirePermission("generator.fuel.manage")
            .Produces<GeneratorFuelReceiptDto>(StatusCodes.Status200OK);

        receipts.MapGet("/", async (Guid? generatorId, int page, int pageSize, ISender sender, CancellationToken ct) =>
            {
                PagedResult<GeneratorFuelReceiptDto> result = await sender.Send(
                    new GetGeneratorFuelReceiptsQuery(generatorId, page < 1 ? 1 : page, pageSize < 1 ? 20 : pageSize), ct);
                return Results.Ok(result);
            })
            .RequirePermission("generator.view")
            .Produces<PagedResult<GeneratorFuelReceiptDto>>(StatusCodes.Status200OK);

        return app;
    }
}
