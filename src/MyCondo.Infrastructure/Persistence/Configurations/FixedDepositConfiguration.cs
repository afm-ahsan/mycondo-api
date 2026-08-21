using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyCondo.Domain.Features.Finance.FinancialAccounts;
using MyCondo.Domain.Features.Finance.FixedDeposits;
using MyCondo.Domain.Features.Finance.Funds;
using MyCondo.Domain.Features.Payments.Ledger;

namespace MyCondo.Infrastructure.Persistence.Configurations;

public sealed class FixedDepositConfiguration : IEntityTypeConfiguration<FixedDeposit>
{
    public void Configure(EntityTypeBuilder<FixedDeposit> builder)
    {
        builder.ToTable("fixed_deposits", schema: "finance");

        builder.HasKey(x => x.Id).HasName("pk_fixed_deposits");
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new FixedDepositId(value))
            .ValueGeneratedNever();

        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.CertificateNumber).IsRequired().HasMaxLength(100);
        builder.Property(x => x.BankName).IsRequired().HasMaxLength(200);
        builder.Property(x => x.BranchName).HasMaxLength(200);

        builder.Property(x => x.FundingFinancialAccountId)
            .HasConversion(id => id.Value, value => new FinancialAccountId(value))
            .IsRequired();

        builder.Property(x => x.ReceivingFinancialAccountId)
            .HasConversion(
                id => id == null ? (Guid?)null : id.Value.Value,
                value => value == null ? (FinancialAccountId?)null : new FinancialAccountId(value.Value));

        builder.Property(x => x.FundId)
            .HasConversion(
                id => id == null ? (Guid?)null : id.Value.Value,
                value => value == null ? (FundId?)null : new FundId(value.Value));

        builder.Property(x => x.Principal).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.InterestRatePercent).HasPrecision(6, 3).IsRequired();
        builder.Property(x => x.CalculationMethod).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.PaymentFrequency).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.StartDate).IsRequired();
        builder.Property(x => x.MaturityDate).IsRequired();
        builder.Property(x => x.ExpectedGrossInterest).HasPrecision(18, 2);
        builder.Property(x => x.ExpectedDeductionRatePercent).HasPrecision(6, 3);
        builder.Property(x => x.Notes).HasMaxLength(1000);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.VoidReason).HasMaxLength(500);
        builder.Property(x => x.Version).IsConcurrencyToken();

        builder.Property(x => x.PredecessorFixedDepositId)
            .HasConversion(
                id => id == null ? (Guid?)null : id.Value.Value,
                value => value == null ? (FixedDepositId?)null : new FixedDepositId(value.Value));

        builder.Property(x => x.SuccessorFixedDepositId)
            .HasConversion(
                id => id == null ? (Guid?)null : id.Value.Value,
                value => value == null ? (FixedDepositId?)null : new FixedDepositId(value.Value));

        builder.Property(x => x.PlacementPostingId)
            .HasConversion(
                id => id == null ? (Guid?)null : id.Value.Value,
                value => value == null ? (LedgerPostingId?)null : new LedgerPostingId(value.Value));

        builder.Property(x => x.RenewalAdjustmentPostingId)
            .HasConversion(
                id => id == null ? (Guid?)null : id.Value.Value,
                value => value == null ? (LedgerPostingId?)null : new LedgerPostingId(value.Value));

        builder.Property(x => x.WithdrawalPostingId)
            .HasConversion(
                id => id == null ? (Guid?)null : id.Value.Value,
                value => value == null ? (LedgerPostingId?)null : new LedgerPostingId(value.Value));

        builder.Property(x => x.VoidReversalPostingId)
            .HasConversion(
                id => id == null ? (Guid?)null : id.Value.Value,
                value => value == null ? (LedgerPostingId?)null : new LedgerPostingId(value.Value));

        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.CreatedBy);
        builder.Property(x => x.UpdatedAtUtc);
        builder.Property(x => x.UpdatedBy);

        builder.HasIndex(x => new { x.TenantId, x.CertificateNumber })
            .IsUnique()
            .HasDatabaseName("ux_fixed_deposits_tenant_id_certificate_number");

        builder.HasIndex(x => new { x.TenantId, x.Status })
            .HasDatabaseName("ix_fixed_deposits_tenant_id_status");

        builder.HasIndex(x => new { x.TenantId, x.FundingFinancialAccountId })
            .HasDatabaseName("ix_fixed_deposits_tenant_id_funding_financial_account_id");

        builder.HasIndex(x => new { x.TenantId, x.FundId })
            .HasDatabaseName("ix_fixed_deposits_tenant_id_fund_id");

        builder.Ignore(x => x.DomainEvents);
    }
}
