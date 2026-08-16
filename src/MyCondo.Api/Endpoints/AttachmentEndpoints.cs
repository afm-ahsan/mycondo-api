using Mediator;
using Microsoft.AspNetCore.Mvc;
using MyCondo.Api.Authorization;
using MyCondo.Application.Features.Attachments.Commands.DeleteAttachment;
using MyCondo.Application.Features.Attachments.Commands.UploadAttachment;
using MyCondo.Application.Features.Attachments.DTOs;
using MyCondo.Application.Features.Attachments.Queries.GetAttachmentContent;
using MyCondo.Application.Features.Attachments.Queries.GetAttachmentsForOwner;

namespace MyCondo.Api.Endpoints;

public static class AttachmentEndpoints
{
    public static IEndpointRouteBuilder MapAttachmentEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder attachments = app.MapGroup("/api/v1/attachments").WithTags("Attachments");

        attachments.MapPost("/", async (
                IFormFile file, [FromForm] string ownerType, [FromForm] Guid ownerId, ISender sender, CancellationToken ct) =>
            {
                await using Stream stream = file.OpenReadStream();
                AttachmentDto result = await sender.Send(
                    new UploadAttachmentCommand(stream, ownerType, ownerId, file.FileName, file.ContentType, file.Length), ct);
                return Results.Ok(result);
            })
            .RequirePermission("document.upload")
            .DisableAntiforgery()
            .Accepts<IFormFile>("multipart/form-data")
            .Produces<AttachmentDto>(StatusCodes.Status200OK);

        attachments.MapGet("/", async (string ownerType, Guid ownerId, ISender sender, CancellationToken ct) =>
            {
                List<AttachmentDto> result = await sender.Send(new GetAttachmentsForOwnerQuery(ownerType, ownerId), ct);
                return Results.Ok(result);
            })
            .RequirePermission("document.view")
            .Produces<List<AttachmentDto>>(StatusCodes.Status200OK);

        attachments.MapGet("/{attachmentId:guid}/content", async (Guid attachmentId, ISender sender, CancellationToken ct) =>
            {
                AttachmentContentDto? content = await sender.Send(new GetAttachmentContentQuery(attachmentId), ct);
                return content is null
                    ? Results.NotFound()
                    : Results.File(content.Content, content.ContentType, content.FileName);
            })
            .RequirePermission("document.view");

        attachments.MapDelete("/{attachmentId:guid}", async (Guid attachmentId, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new DeleteAttachmentCommand(attachmentId), ct);
                return Results.NoContent();
            })
            .RequirePermission("document.upload")
            .Produces(StatusCodes.Status204NoContent);

        return app;
    }
}
