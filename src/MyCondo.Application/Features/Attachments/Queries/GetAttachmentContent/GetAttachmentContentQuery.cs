using Mediator;
using MyCondo.Application.Common.Abstractions;

namespace MyCondo.Application.Features.Attachments.Queries.GetAttachmentContent;

public sealed record GetAttachmentContentQuery(Guid AttachmentId) : IRequest<AttachmentContentDto?>, ILifecycleReadOperation;

public sealed record AttachmentContentDto(Stream Content, string ContentType, string FileName);
