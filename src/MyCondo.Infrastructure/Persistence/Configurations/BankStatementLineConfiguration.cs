using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyCondo.Domain.Features.Finance.BankReconciliations;
using MyCondo.Domain.Features.Payments.Ledger;

namespace MyCondo.Infrastructure.Persistence.Configurations;

public sealed class BankStatementLineConfiguration : IEntityTypeConfiguration<BankStatementLine>
{
    public void Configure(EntityTypeBuilder<BankStatementLine> builder)
    {
        builder.ToTable("bank_statement_lines", schema: "finance");

        builder.HasKey(x => x.Id).HasName("pk_bank_statement_lines");
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new BankStatementLineId(value))
            .ValueGeneratedNever();

        builder.Property(x => x.TenantId).IsRequired();

        builder.Property(x => x.BankReconciliationId)
            .HasConversion(id => id.Value, value => new BankReconciliationId(value))
            .IsRequired();

        builder.Property(x => x.TransactionDate).IsRequired();
        builder.Property(x => x.Description).IsRequired().HasMaxLength(500);
        builder.Property(x => x.Amount).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.Property(x => x.MatchedLedgerEntryId)
            .HasConversion(
                id => id == null ? (Guid?)null : id.Value.Value,
                value => value == null ? (LedgerEntryId?)null : new LedgerEntryId(value.Value));

        builder.Property(x => x.AdjustmentPostingId)
            .HasConversion(
                id => id == null ? (Guid?)null : id.Value.Value,
                value => value == null ? (LedgerPostingId?)null : new LedgerPostingId(value.Value));

        builder.Property(x => x.ResolutionNotes).HasMaxLength(500);

        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.CreatedBy);
        builder.Property(x => x.UpdatedAtUtc);
        builder.Property(x => x.UpdatedBy);

        builder.HasIndex(x => new { x.TenantId, x.BankReconciliationId })
            .HasDatabaseName("ix_bank_statement_lines_tenant_id_bank_reconciliation_id");

        builder.Ignore(x => x.DomainEvents);
    }
}
