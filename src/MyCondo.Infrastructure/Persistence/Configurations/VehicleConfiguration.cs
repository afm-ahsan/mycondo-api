using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyCondo.Domain.Features.Property.Flats;
using MyCondo.Domain.Features.Security.Vehicles;

namespace MyCondo.Infrastructure.Persistence.Configurations;

public sealed class VehicleConfiguration : IEntityTypeConfiguration<Vehicle>
{
    public void Configure(EntityTypeBuilder<Vehicle> builder)
    {
        builder.ToTable("vehicles", schema: "security");

        builder.HasKey(x => x.Id).HasName("pk_vehicles");
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new VehicleId(value))
            .ValueGeneratedNever();

        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.RegistrationNumber).IsRequired().HasMaxLength(30);
        builder.Property(x => x.VehicleType).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.Make).HasMaxLength(60);
        builder.Property(x => x.Model).HasMaxLength(60);
        builder.Property(x => x.Color).HasMaxLength(30);
        builder.Property(x => x.OwnershipCategory).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.FlatId)
            .HasConversion(
                id => id.HasValue ? id.Value.Value : (Guid?)null,
                value => value.HasValue ? new FlatId(value.Value) : (FlatId?)null);
        builder.Property(x => x.IsBlocked).IsRequired();
        builder.Property(x => x.BlockReason).HasMaxLength(400);
        builder.Property(x => x.Version).IsConcurrencyToken();

        builder.HasIndex(x => new { x.TenantId, x.RegistrationNumber })
            .IsUnique()
            .HasDatabaseName("ux_vehicles_tenant_id_registration_number");

        builder.HasIndex(x => new { x.TenantId, x.FlatId })
            .HasDatabaseName("ix_vehicles_tenant_id_flat_id");

        builder.HasQueryFilter(x => x.DeletedAtUtc == null);
        builder.Ignore(x => x.DomainEvents);
    }
}
