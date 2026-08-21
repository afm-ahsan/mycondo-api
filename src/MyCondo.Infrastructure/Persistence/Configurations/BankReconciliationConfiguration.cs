using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyCondo.Domain.Features.Finance.BankReconciliations;
using MyCondo.Domain.Features.Finance.FinancialAccounts;

namespace MyCondo.Infrastructure.Persistence.Configurations;

public sealed class BankReconciliationConfiguration : IEntityTypeConfiguration<BankReconciliation>
{
    public void Configure(EntityTypeBuilder<BankReconciliation> builder)
    {
        builder.ToTable("bank_reconciliations", schema: "finance");

        builder.HasKey(x => x.Id).HasName("pk_bank_reconciliations");
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new BankReconciliationId(value))
            .ValueGeneratedNever();

        builder.Property(x => x.TenantId).IsRequired();

        builder.Property(x => x.FinancialAccountId)
            .HasConversion(id => id.Value, value => new FinancialAccountId(value))
            .IsRequired();

        builder.Property(x => x.StatementDate).IsRequired();
        builder.Property(x => x.StatementBalance).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.OpeningLedgerBalance).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.ReconciledAtUtc);
        builder.Property(x => x.ReconciledBy);

        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.CreatedBy);
        builder.Property(x => x.UpdatedAtUtc);
        builder.Property(x => x.UpdatedBy);

        builder.HasIndex(x => new { x.TenantId, x.FinancialAccountId, x.StatementDate })
            .HasDatabaseName("ix_bank_reconciliations_tenant_id_financial_account_id_statement_date");

        builder.Ignore(x => x.DomainEvents);
    }
}
