using Mediator;

namespace MyCondo.Application.Features.Attachments.Queries.GetAttachmentContent;

public sealed record GetAttachmentContentQuery(Guid AttachmentId) : IRequest<AttachmentContentDto?>;

public sealed record AttachmentContentDto(Stream Content, string ContentType, string FileName);
