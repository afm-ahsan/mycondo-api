using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyCondo.Domain.Features.Finance.ChartOfAccounts;
using MyCondo.Domain.Features.Finance.FinancialAccounts;
using MyCondo.Domain.Features.Finance.Funds;

namespace MyCondo.Infrastructure.Persistence.Configurations;

public sealed class FinancialAccountConfiguration : IEntityTypeConfiguration<FinancialAccount>
{
    public void Configure(EntityTypeBuilder<FinancialAccount> builder)
    {
        builder.ToTable("financial_accounts", schema: "finance");

        builder.HasKey(x => x.Id).HasName("pk_financial_accounts");
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new FinancialAccountId(value))
            .ValueGeneratedNever();

        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
        builder.Property(x => x.AccountType).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(x => x.BankName).HasMaxLength(200);
        builder.Property(x => x.BranchName).HasMaxLength(200);
        builder.Property(x => x.AccountNumber).HasMaxLength(100);
        builder.Property(x => x.Notes).HasMaxLength(1000);
        builder.Property(x => x.IsActive).IsRequired();

        builder.Property(x => x.ChartOfAccountId)
            .HasConversion(id => id.Value, value => new ChartOfAccountId(value))
            .IsRequired();

        builder.Property(x => x.FundId)
            .HasConversion(
                id => id == null ? (Guid?)null : id.Value.Value,
                value => value == null ? (FundId?)null : new FundId(value.Value));

        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.CreatedBy);
        builder.Property(x => x.UpdatedAtUtc);
        builder.Property(x => x.UpdatedBy);

        builder.HasIndex(x => new { x.TenantId, x.IsActive })
            .HasDatabaseName("ix_financial_accounts_tenant_id_is_active");

        builder.Ignore(x => x.DomainEvents);
    }
}
