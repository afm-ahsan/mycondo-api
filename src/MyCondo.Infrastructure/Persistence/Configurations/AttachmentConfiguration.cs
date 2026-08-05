using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyCondo.Domain.Features.Attachments;

namespace MyCondo.Infrastructure.Persistence.Configurations;

public sealed class AttachmentConfiguration : IEntityTypeConfiguration<Attachment>
{
    public void Configure(EntityTypeBuilder<Attachment> builder)
    {
        builder.ToTable("attachments", schema: "documents");

        builder.HasKey(x => x.Id).HasName("pk_attachments");
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new AttachmentId(value))
            .ValueGeneratedNever();

        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.OwnerType).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(x => x.OwnerId).IsRequired();
        builder.Property(x => x.StorageKey).IsRequired().HasMaxLength(1024);
        builder.Property(x => x.FileName).IsRequired().HasMaxLength(260);
        builder.Property(x => x.ContentType).IsRequired().HasMaxLength(150);
        builder.Property(x => x.SizeBytes).IsRequired();

        builder.HasIndex(x => new { x.TenantId, x.OwnerType, x.OwnerId })
            .HasDatabaseName("ix_attachments_tenant_id_owner_type_owner_id");

        builder.HasQueryFilter(x => x.DeletedAtUtc == null);
        builder.Ignore(x => x.DomainEvents);
    }
}
