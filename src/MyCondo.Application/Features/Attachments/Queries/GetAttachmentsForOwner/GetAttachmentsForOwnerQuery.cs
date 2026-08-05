using Mediator;
using MyCondo.Application.Features.Attachments.DTOs;

namespace MyCondo.Application.Features.Attachments.Queries.GetAttachmentsForOwner;

public sealed record GetAttachmentsForOwnerQuery(string OwnerType, Guid OwnerId) : IRequest<List<AttachmentDto>>;
