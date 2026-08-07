using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyCondo.Domain.Features.Operations.GasCylinderSuppliers;

namespace MyCondo.Infrastructure.Persistence.Configurations;

public sealed class GasCylinderSupplierConfiguration : IEntityTypeConfiguration<GasCylinderSupplier>
{
    public void Configure(EntityTypeBuilder<GasCylinderSupplier> builder)
    {
        builder.ToTable("gas_cylinder_suppliers", schema: "operations");

        builder.HasKey(x => x.Id).HasName("pk_gas_cylinder_suppliers");
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new GasCylinderSupplierId(value))
            .ValueGeneratedNever();

        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
        builder.Property(x => x.ContactPhone).HasMaxLength(30);
        builder.Property(x => x.ContactEmail).HasMaxLength(200);
        builder.Property(x => x.Address).HasMaxLength(500);
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.Version).IsConcurrencyToken();

        builder.HasIndex(x => new { x.TenantId, x.IsActive })
            .HasDatabaseName("ix_gas_cylinder_suppliers_tenant_id_is_active");

        builder.Ignore(x => x.DomainEvents);
    }
}
