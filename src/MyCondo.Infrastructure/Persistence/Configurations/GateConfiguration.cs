using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Domain.Features.Property.Gates;

namespace MyCondo.Infrastructure.Persistence.Configurations;

public sealed class GateConfiguration : IEntityTypeConfiguration<Gate>
{
    public void Configure(EntityTypeBuilder<Gate> builder)
    {
        builder.ToTable("gates", schema: "property");

        builder.HasKey(x => x.Id).HasName("pk_gates");
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new GateId(value))
            .ValueGeneratedNever();

        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.BuildingId)
            .HasConversion(id => id.Value, value => new BuildingId(value))
            .IsRequired();
        builder.Property(x => x.Name).IsRequired().HasMaxLength(100);

        builder.HasIndex(x => new { x.TenantId, x.BuildingId, x.Name })
            .IsUnique()
            .HasDatabaseName("ux_gates_tenant_id_building_id_name");

        builder.Ignore(x => x.DomainEvents);
    }
}
