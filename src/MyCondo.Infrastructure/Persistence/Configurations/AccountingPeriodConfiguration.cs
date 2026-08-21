using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyCondo.Domain.Features.Finance.AccountingPeriods;
using MyCondo.Domain.Features.Finance.FinancialYears;

namespace MyCondo.Infrastructure.Persistence.Configurations;

public sealed class AccountingPeriodConfiguration : IEntityTypeConfiguration<AccountingPeriod>
{
    public void Configure(EntityTypeBuilder<AccountingPeriod> builder)
    {
        builder.ToTable("accounting_periods", schema: "finance");

        builder.HasKey(x => x.Id).HasName("pk_accounting_periods");
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new AccountingPeriodId(value))
            .ValueGeneratedNever();

        builder.Property(x => x.TenantId).IsRequired();

        builder.Property(x => x.FinancialYearId)
            .HasConversion(id => id.Value, value => new FinancialYearId(value))
            .IsRequired();

        builder.Property(x => x.Name).IsRequired().HasMaxLength(100);
        builder.Property(x => x.StartDate).IsRequired();
        builder.Property(x => x.EndDate).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(10).IsRequired();

        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.CreatedBy);
        builder.Property(x => x.UpdatedAtUtc);
        builder.Property(x => x.UpdatedBy);

        builder.HasIndex(x => new { x.TenantId, x.FinancialYearId })
            .HasDatabaseName("ix_accounting_periods_tenant_id_financial_year_id");

        builder.HasIndex(x => new { x.TenantId, x.StartDate, x.EndDate })
            .HasDatabaseName("ix_accounting_periods_tenant_id_start_date_end_date");

        builder.Ignore(x => x.DomainEvents);
    }
}
