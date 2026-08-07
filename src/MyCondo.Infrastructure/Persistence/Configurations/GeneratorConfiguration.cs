using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyCondo.Domain.Features.Operations.Generators;
using MyCondo.Domain.Features.Property.Buildings;

namespace MyCondo.Infrastructure.Persistence.Configurations;

public sealed class GeneratorConfiguration : IEntityTypeConfiguration<Generator>
{
    public void Configure(EntityTypeBuilder<Generator> builder)
    {
        builder.ToTable("generators", schema: "operations");

        builder.HasKey(x => x.Id).HasName("pk_generators");
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new GeneratorId(value))
            .ValueGeneratedNever();

        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.BuildingId)
            .HasConversion(id => id.Value, value => new BuildingId(value))
            .IsRequired();
        builder.Property(x => x.Name).IsRequired().HasMaxLength(120);
        builder.Property(x => x.Model).HasMaxLength(120);
        builder.Property(x => x.CapacityKva).HasPrecision(10, 2);
        builder.Property(x => x.Location).HasMaxLength(200);
        builder.Property(x => x.CurrentHourMeterReading).HasPrecision(12, 2).IsRequired();
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.Version).IsConcurrencyToken();

        builder.HasIndex(x => new { x.TenantId, x.BuildingId })
            .HasDatabaseName("ix_generators_tenant_id_building_id");

        builder.Ignore(x => x.DomainEvents);
    }
}
