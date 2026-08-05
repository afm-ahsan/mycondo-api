using Mediator;
using MyCondo.Application.Features.Attachments.DTOs;

namespace MyCondo.Application.Features.Attachments.Commands.RecordAttachment;

/// <summary>
/// Records metadata for a file the caller has already placed in object storage — this slice does not
/// wire an actual upload/pre-signed-URL endpoint (see <c>Attachment</c>'s doc comment). <c>StorageKey</c>
/// is trusted as given; validating that it actually points at a real, tenant-owned object is a
/// follow-up once real storage integration lands.
/// </summary>
public sealed record RecordAttachmentCommand(
    string OwnerType,
    Guid OwnerId,
    string StorageKey,
    string FileName,
    string ContentType,
    long SizeBytes
) : IRequest<AttachmentDto>;
