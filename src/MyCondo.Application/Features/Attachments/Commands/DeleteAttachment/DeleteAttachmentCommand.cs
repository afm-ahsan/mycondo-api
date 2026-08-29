using Mediator;
using MyCondo.Application.Common.Abstractions;

namespace MyCondo.Application.Features.Attachments.Commands.DeleteAttachment;

public sealed record DeleteAttachmentCommand(Guid AttachmentId) : IRequest<Unit>, ILifecycleWriteOperation;
