using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyCondo.Domain.Features.Property.Buildings;

namespace MyCondo.Infrastructure.Persistence.Configurations;

public sealed class BuildingConfiguration : IEntityTypeConfiguration<Building>
{
    public void Configure(EntityTypeBuilder<Building> builder)
    {
        builder.ToTable("buildings", schema: "property");

        builder.HasKey(x => x.Id).HasName("pk_buildings");
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new BuildingId(value))
            .ValueGeneratedNever();

        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Address).HasMaxLength(400);
        builder.Property(x => x.Version).IsConcurrencyToken();

        builder.HasIndex(x => new { x.TenantId, x.Name })
            .IsUnique()
            .HasDatabaseName("ux_buildings_tenant_id_name");

        builder.HasQueryFilter(x => x.DeletedAtUtc == null);
        builder.Ignore(x => x.DomainEvents);
    }
}
