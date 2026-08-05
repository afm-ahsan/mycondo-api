using Mediator;
using MyCondo.Api.Authorization;
using MyCondo.Application.Features.Attachments.Commands.RecordAttachment;
using MyCondo.Application.Features.Attachments.DTOs;
using MyCondo.Application.Features.Attachments.Queries.GetAttachmentsForOwner;

namespace MyCondo.Api.Endpoints;

public static class AttachmentEndpoints
{
    public static IEndpointRouteBuilder MapAttachmentEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder attachments = app.MapGroup("/api/v1/attachments").WithTags("Attachments");

        attachments.MapPost("/", async (RecordAttachmentCommand command, ISender sender, CancellationToken ct) =>
            {
                AttachmentDto result = await sender.Send(command, ct);
                return Results.Ok(result);
            })
            .RequirePermission("document.upload")
            .Produces<AttachmentDto>(StatusCodes.Status200OK);

        attachments.MapGet("/", async (string ownerType, Guid ownerId, ISender sender, CancellationToken ct) =>
            {
                List<AttachmentDto> result = await sender.Send(new GetAttachmentsForOwnerQuery(ownerType, ownerId), ct);
                return Results.Ok(result);
            })
            .RequirePermission("document.view")
            .Produces<List<AttachmentDto>>(StatusCodes.Status200OK);

        return app;
    }
}
