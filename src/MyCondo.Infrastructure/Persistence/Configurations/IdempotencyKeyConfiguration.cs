using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyCondo.Domain.Features.Payments.Idempotency;

namespace MyCondo.Infrastructure.Persistence.Configurations;

public sealed class IdempotencyKeyConfiguration : IEntityTypeConfiguration<IdempotencyKey>
{
    public void Configure(EntityTypeBuilder<IdempotencyKey> builder)
    {
        builder.ToTable("idempotency_keys", schema: "payments");

        builder.HasKey(x => x.Id).HasName("pk_idempotency_keys");
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new IdempotencyKeyId(value))
            .ValueGeneratedNever();

        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.Key).IsRequired().HasMaxLength(200);
        builder.Property(x => x.RequestPath).IsRequired().HasMaxLength(300);
        builder.Property(x => x.RequestHash).IsRequired().HasMaxLength(128);
        builder.Property(x => x.ResponseStatusCode).IsRequired();
        builder.Property(x => x.ResponseBody).IsRequired();

        builder.HasIndex(x => new { x.TenantId, x.Key, x.RequestPath })
            .IsUnique()
            .HasDatabaseName("ux_idempotency_keys_tenant_id_key_request_path");

        builder.Ignore(x => x.DomainEvents);
    }
}
