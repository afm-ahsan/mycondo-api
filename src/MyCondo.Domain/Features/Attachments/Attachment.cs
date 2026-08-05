using MyCondo.Domain.Common;

namespace MyCondo.Domain.Features.Attachments;

/// <summary>
/// Generic file-metadata pointer shared by every register's "photo"/"identity document" field, so
/// registers don't each reinvent file handling. Records metadata only — this slice does not wire an
/// actual object-storage backend (S3/MinIO isn't added to <c>Directory.Packages.props</c> yet, per its
/// own "added when Documents module needs it" note); <see cref="StorageKey"/> is supplied by the
/// caller today. Wiring real upload/pre-signed-URL retrieval is a follow-up, not built here.
/// </summary>
public sealed class Attachment : AggregateRoot<AttachmentId>, IAuditable, ISoftDeletable, ITenantScoped
{
    public Guid TenantId { get; private set; }
    public AttachmentOwnerType OwnerType { get; private set; }
    public Guid OwnerId { get; private set; }
    public string StorageKey { get; private set; }
    public string FileName { get; private set; }
    public string ContentType { get; private set; }
    public long SizeBytes { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public Guid? UpdatedBy { get; set; }

    public DateTimeOffset? DeletedAtUtc { get; set; }
    public Guid? DeletedBy { get; set; }

    private Attachment()
    {
        StorageKey = null!;
        FileName = null!;
        ContentType = null!;
    }

    private Attachment(
        AttachmentId id,
        Guid tenantId,
        AttachmentOwnerType ownerType,
        Guid ownerId,
        string storageKey,
        string fileName,
        string contentType,
        long sizeBytes,
        DateTimeOffset nowUtc) : base(id)
    {
        TenantId = tenantId;
        OwnerType = ownerType;
        OwnerId = ownerId;
        StorageKey = storageKey;
        FileName = fileName;
        ContentType = contentType;
        SizeBytes = sizeBytes;
        CreatedAtUtc = nowUtc;
    }

    public static Attachment Record(
        Guid tenantId,
        AttachmentOwnerType ownerType,
        Guid ownerId,
        string storageKey,
        string fileName,
        string contentType,
        long sizeBytes,
        DateTimeOffset nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storageKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        }

        if (ownerId == Guid.Empty)
        {
            throw new ArgumentException("OwnerId is required.", nameof(ownerId));
        }

        if (sizeBytes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sizeBytes), "SizeBytes must be positive.");
        }

        return new Attachment(
            AttachmentId.New(), tenantId, ownerType, ownerId, storageKey.Trim(), fileName.Trim(),
            contentType.Trim(), sizeBytes, nowUtc);
    }

    public void Delete(DateTimeOffset nowUtc, Guid? deletedBy)
    {
        if (DeletedAtUtc is not null)
        {
            return;
        }

        DeletedAtUtc = nowUtc;
        DeletedBy = deletedBy;
    }
}
