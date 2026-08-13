using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyCondo.Domain.Features.Property.FlatOwnerships;
using MyCondo.Domain.Features.Property.Flats;

namespace MyCondo.Infrastructure.Persistence.Configurations;

public sealed class FlatOwnershipConfiguration : IEntityTypeConfiguration<FlatOwnership>
{
    public void Configure(EntityTypeBuilder<FlatOwnership> builder)
    {
        builder.ToTable("flat_ownerships", schema: "property");

        builder.HasKey(x => x.Id).HasName("pk_flat_ownerships");
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new FlatOwnershipId(value))
            .ValueGeneratedNever();

        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.ResidentId).IsRequired();
        builder.Property(x => x.FlatId)
            .HasConversion(id => id.Value, value => new FlatId(value))
            .IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.StartDate).IsRequired();
        builder.Property(x => x.Version).IsConcurrencyToken();

        builder.HasIndex(x => new { x.TenantId, x.ResidentId })
            .HasDatabaseName("ix_flat_ownerships_tenant_id_resident_id");

        builder.HasIndex(x => new { x.TenantId, x.FlatId })
            .HasDatabaseName("ix_flat_ownerships_tenant_id_flat_id");

        // Partial/filtered: only one Active ownership row per (Resident, Flat) at a time — re-granting
        // after a prior ownership ended is still allowed (a new row, old one stays Ended for history).
        builder.HasIndex(x => new { x.TenantId, x.ResidentId, x.FlatId })
            .IsUnique()
            .HasFilter("\"status\" = 'Active'")
            .HasDatabaseName("ux_flat_ownerships_tenant_id_resident_id_flat_id_active");

        builder.Ignore(x => x.DomainEvents);
    }
}
