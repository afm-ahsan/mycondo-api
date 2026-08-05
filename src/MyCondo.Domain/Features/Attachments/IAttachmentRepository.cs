namespace MyCondo.Domain.Features.Attachments;

public interface IAttachmentRepository
{
    Task<Attachment?> GetByIdAsync(AttachmentId id, CancellationToken cancellationToken);

    Task<List<Attachment>> GetForOwnerAsync(
        Guid tenantId, AttachmentOwnerType ownerType, Guid ownerId, CancellationToken cancellationToken);

    void Add(Attachment attachment);
}
