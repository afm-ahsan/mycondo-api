namespace MyCondo.Application.Features.Attachments.DTOs;

public sealed record AttachmentDto(
    Guid AttachmentId,
    string OwnerType,
    Guid OwnerId,
    string StorageKey,
    string FileName,
    string ContentType,
    long SizeBytes,
    DateTimeOffset CreatedAtUtc);
