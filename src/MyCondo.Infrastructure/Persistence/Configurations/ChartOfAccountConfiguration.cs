using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyCondo.Domain.Features.Finance.ChartOfAccounts;

namespace MyCondo.Infrastructure.Persistence.Configurations;

public sealed class ChartOfAccountConfiguration : IEntityTypeConfiguration<ChartOfAccount>
{
    public void Configure(EntityTypeBuilder<ChartOfAccount> builder)
    {
        builder.ToTable("chart_of_accounts", schema: "finance");

        builder.HasKey(x => x.Id).HasName("pk_chart_of_accounts");
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new ChartOfAccountId(value))
            .ValueGeneratedNever();

        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.Code).IsRequired().HasMaxLength(30);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Category).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.NormalBalance).HasConversion<string>().HasMaxLength(10).IsRequired();
        builder.Property(x => x.IsSystemAccount).IsRequired();
        builder.Property(x => x.IsActive).IsRequired();

        builder.Property(x => x.ParentAccountId)
            .HasConversion(
                id => id.HasValue ? id.Value.Value : (Guid?)null,
                value => value.HasValue ? new ChartOfAccountId(value.Value) : (ChartOfAccountId?)null);

        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.CreatedBy);
        builder.Property(x => x.UpdatedAtUtc);
        builder.Property(x => x.UpdatedBy);

        builder.HasIndex(x => new { x.TenantId, x.Code })
            .IsUnique()
            .HasDatabaseName("ux_chart_of_accounts_tenant_id_code");

        builder.Ignore(x => x.DomainEvents);
    }
}
