using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Domain.Features.Property.Flats;

namespace MyCondo.Infrastructure.Persistence.Configurations;

public sealed class FlatConfiguration : IEntityTypeConfiguration<Flat>
{
    public void Configure(EntityTypeBuilder<Flat> builder)
    {
        builder.ToTable("flats", schema: "property");

        builder.HasKey(x => x.Id).HasName("pk_flats");
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new FlatId(value))
            .ValueGeneratedNever();

        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.BuildingId)
            .HasConversion(id => id.Value, value => new BuildingId(value))
            .IsRequired();
        builder.Property(x => x.FlatNumber).IsRequired().HasMaxLength(50);
        builder.Property(x => x.FloorNumber);
        builder.Property(x => x.FlatType).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.AreaSqFt).HasPrecision(10, 2);
        builder.Property(x => x.Version).IsConcurrencyToken();

        builder.HasIndex(x => new { x.TenantId, x.BuildingId, x.FlatNumber })
            .IsUnique()
            .HasDatabaseName("ux_flats_tenant_id_building_id_flat_number");

        builder.HasIndex(x => new { x.TenantId, x.BuildingId })
            .HasDatabaseName("ix_flats_tenant_id_building_id");

        builder.HasQueryFilter(x => x.DeletedAtUtc == null);
        builder.Ignore(x => x.DomainEvents);
    }
}
