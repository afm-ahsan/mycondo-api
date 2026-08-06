using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyCondo.Domain.Features.Utilities.RatePlans;

namespace MyCondo.Infrastructure.Persistence.Configurations;

public sealed class RateSlabConfiguration : IEntityTypeConfiguration<RateSlab>
{
    public void Configure(EntityTypeBuilder<RateSlab> builder)
    {
        builder.ToTable("rate_slabs", schema: "utilities");

        builder.HasKey(x => x.Id).HasName("pk_rate_slabs");
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new RateSlabId(value))
            .ValueGeneratedNever();

        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.RatePlanId)
            .HasConversion(id => id.Value, value => new RatePlanId(value))
            .IsRequired();
        builder.Property(x => x.SlabOrder).IsRequired();
        builder.Property(x => x.FromUnits).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.ToUnits).HasPrecision(18, 2);
        builder.Property(x => x.RatePerUnit).HasPrecision(18, 4).IsRequired();

        builder.HasIndex(x => new { x.TenantId, x.RatePlanId, x.SlabOrder })
            .IsUnique()
            .HasDatabaseName("ux_rate_slabs_tenant_id_rate_plan_id_slab_order");
    }
}
