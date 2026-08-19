using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyCondo.Domain.Features.Finance.FinancialAccounts;
using MyCondo.Domain.Features.Finance.FixedDeposits;
using MyCondo.Domain.Features.Payments.Ledger;

namespace MyCondo.Infrastructure.Persistence.Configurations;

public sealed class FixedDepositInterestReceiptConfiguration : IEntityTypeConfiguration<FixedDepositInterestReceipt>
{
    public void Configure(EntityTypeBuilder<FixedDepositInterestReceipt> builder)
    {
        builder.ToTable("fixed_deposit_interest_receipts", schema: "finance");

        builder.HasKey(x => x.Id).HasName("pk_fixed_deposit_interest_receipts");
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new FixedDepositInterestReceiptId(value))
            .ValueGeneratedNever();

        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.FixedDepositId)
            .HasConversion(id => id.Value, value => new FixedDepositId(value))
            .IsRequired();

        builder.Property(x => x.AccountingDate).IsRequired();
        builder.Property(x => x.GrossAmount).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.DeductionAmount).HasPrecision(18, 2).IsRequired();
        builder.Ignore(x => x.NetAmount);

        builder.Property(x => x.ReceivingFinancialAccountId)
            .HasConversion(id => id.Value, value => new FinancialAccountId(value))
            .IsRequired();

        builder.Property(x => x.ReferenceNumber).HasMaxLength(100);
        builder.Property(x => x.Notes).HasMaxLength(1000);
        builder.Property(x => x.IsReversed).IsRequired();

        builder.Property(x => x.PostingId)
            .HasConversion(id => id.Value, value => new LedgerPostingId(value))
            .IsRequired();

        builder.Property(x => x.ReversalPostingId)
            .HasConversion(
                id => id == null ? (Guid?)null : id.Value.Value,
                value => value == null ? (LedgerPostingId?)null : new LedgerPostingId(value.Value));

        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.CreatedBy);
        builder.Property(x => x.UpdatedAtUtc);
        builder.Property(x => x.UpdatedBy);

        builder.HasIndex(x => new { x.TenantId, x.FixedDepositId })
            .HasDatabaseName("ix_fd_interest_receipts_tenant_id_fixed_deposit_id");

        builder.Ignore(x => x.DomainEvents);
    }
}
