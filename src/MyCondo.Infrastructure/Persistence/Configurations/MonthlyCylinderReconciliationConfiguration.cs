using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyCondo.Domain.Features.Operations.MonthlyCylinderReconciliations;

namespace MyCondo.Infrastructure.Persistence.Configurations;

public sealed class MonthlyCylinderReconciliationConfiguration : IEntityTypeConfiguration<MonthlyCylinderReconciliation>
{
    public void Configure(EntityTypeBuilder<MonthlyCylinderReconciliation> builder)
    {
        builder.ToTable("monthly_cylinder_reconciliations", schema: "operations");

        builder.HasKey(x => x.Id).HasName("pk_monthly_cylinder_reconciliations");
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new MonthlyCylinderReconciliationId(value))
            .ValueGeneratedNever();

        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.CylinderType).IsRequired().HasMaxLength(50);
        builder.Property(x => x.PeriodMonth).IsRequired();
        builder.Property(x => x.OpeningStock).IsRequired();
        builder.Property(x => x.TotalReceived).IsRequired();
        builder.Property(x => x.TotalIssued).IsRequired();
        builder.Property(x => x.TotalEmptyReturned).IsRequired();
        builder.Property(x => x.ClosingStock).IsRequired();
        builder.Property(x => x.VarianceQuantity).IsRequired();
        builder.Property(x => x.Remarks).HasMaxLength(500);
        builder.Property(x => x.ReconciledBy);
        builder.Property(x => x.ReconciledAtUtc).IsRequired();

        builder.HasIndex(x => new { x.TenantId, x.CylinderType, x.PeriodMonth })
            .IsUnique()
            .HasDatabaseName("ux_monthly_cylinder_reconciliations_tenant_id_cylinder_type_period_month");
    }
}
