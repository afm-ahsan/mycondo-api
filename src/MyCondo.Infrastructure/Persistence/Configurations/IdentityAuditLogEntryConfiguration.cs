using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyCondo.Domain.Features.Identity.Audit;

namespace MyCondo.Infrastructure.Persistence.Configurations;

public sealed class IdentityAuditLogEntryConfiguration : IEntityTypeConfiguration<IdentityAuditLogEntry>
{
    public void Configure(EntityTypeBuilder<IdentityAuditLogEntry> builder)
    {
        builder.ToTable("identity_audit_log", schema: "identity");

        builder.HasKey(x => x.Id).HasName("pk_identity_audit_log");
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new IdentityAuditLogEntryId(value))
            .ValueGeneratedNever();

        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.OccurredAtUtc).IsRequired();
        builder.Property(x => x.Action).IsRequired().HasMaxLength(120);
        builder.Property(x => x.TargetType).HasMaxLength(120);
        builder.Property(x => x.TargetId).HasMaxLength(120);
        builder.Property(x => x.Metadata).HasColumnType("jsonb");
        builder.Property(x => x.CorrelationId).HasMaxLength(64);

        builder.HasIndex(x => new { x.TenantId, x.OccurredAtUtc })
            .HasDatabaseName("ix_identity_audit_log_tenant_id_occurred_at_utc");
    }
}
