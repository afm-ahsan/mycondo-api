using Mediator;

namespace MyCondo.Application.Features.Attachments.Commands.DeleteAttachment;

public sealed record DeleteAttachmentCommand(Guid AttachmentId) : IRequest<Unit>;
