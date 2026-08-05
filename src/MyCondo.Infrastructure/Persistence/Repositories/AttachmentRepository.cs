using Microsoft.EntityFrameworkCore;
using MyCondo.Domain.Features.Attachments;

namespace MyCondo.Infrastructure.Persistence.Repositories;

public sealed class AttachmentRepository(MyCondoDbContext db) : IAttachmentRepository
{
    public Task<Attachment?> GetByIdAsync(AttachmentId id, CancellationToken cancellationToken) =>
        db.Set<Attachment>().FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public Task<List<Attachment>> GetForOwnerAsync(
        Guid tenantId, AttachmentOwnerType ownerType, Guid ownerId, CancellationToken cancellationToken) =>
        db.Set<Attachment>()
            .Where(a => a.TenantId == tenantId && a.OwnerType == ownerType && a.OwnerId == ownerId)
            .OrderByDescending(a => a.CreatedAtUtc)
            .ToListAsync(cancellationToken);

    public void Add(Attachment attachment) => db.Set<Attachment>().Add(attachment);
}
