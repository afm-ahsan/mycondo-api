using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyCondo.Domain.Features.Finance.FinancialYears;

namespace MyCondo.Infrastructure.Persistence.Configurations;

public sealed class FinancialYearConfiguration : IEntityTypeConfiguration<FinancialYear>
{
    public void Configure(EntityTypeBuilder<FinancialYear> builder)
    {
        builder.ToTable("financial_years", schema: "finance");

        builder.HasKey(x => x.Id).HasName("pk_financial_years");
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new FinancialYearId(value))
            .ValueGeneratedNever();

        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.Name).IsRequired().HasMaxLength(100);
        builder.Property(x => x.StartDate).IsRequired();
        builder.Property(x => x.EndDate).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(10).IsRequired();

        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.CreatedBy);
        builder.Property(x => x.UpdatedAtUtc);
        builder.Property(x => x.UpdatedBy);

        builder.HasIndex(x => new { x.TenantId, x.StartDate, x.EndDate })
            .HasDatabaseName("ix_financial_years_tenant_id_start_date_end_date");

        builder.Ignore(x => x.DomainEvents);
    }
}
