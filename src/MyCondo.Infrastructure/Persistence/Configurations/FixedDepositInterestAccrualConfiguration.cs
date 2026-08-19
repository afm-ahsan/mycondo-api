using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyCondo.Domain.Features.Finance.FixedDeposits;
using MyCondo.Domain.Features.Payments.Ledger;

namespace MyCondo.Infrastructure.Persistence.Configurations;

public sealed class FixedDepositInterestAccrualConfiguration : IEntityTypeConfiguration<FixedDepositInterestAccrual>
{
    public void Configure(EntityTypeBuilder<FixedDepositInterestAccrual> builder)
    {
        builder.ToTable("fixed_deposit_interest_accruals", schema: "finance");

        builder.HasKey(x => x.Id).HasName("pk_fixed_deposit_interest_accruals");
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new FixedDepositInterestAccrualId(value))
            .ValueGeneratedNever();

        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.FixedDepositId)
            .HasConversion(id => id.Value, value => new FixedDepositId(value))
            .IsRequired();

        builder.Property(x => x.PeriodStart).IsRequired();
        builder.Property(x => x.PeriodEnd).IsRequired();
        builder.Property(x => x.AccountingDate).IsRequired();
        builder.Property(x => x.GrossAmount).HasPrecision(18, 2).IsRequired();
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
            .HasDatabaseName("ix_fd_interest_accruals_tenant_id_fixed_deposit_id");

        builder.Ignore(x => x.DomainEvents);
    }
}
