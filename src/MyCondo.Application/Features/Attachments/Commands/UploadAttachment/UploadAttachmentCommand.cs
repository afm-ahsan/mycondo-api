using Mediator;
using MyCondo.Application.Features.Attachments.DTOs;

namespace MyCondo.Application.Features.Attachments.Commands.UploadAttachment;

public sealed record UploadAttachmentCommand(
    Stream Content,
    string OwnerType,
    Guid OwnerId,
    string FileName,
    string ContentType,
    long SizeBytes
) : IRequest<AttachmentDto>;
