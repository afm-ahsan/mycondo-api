using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyCondo.Domain.Features.Expenses.Expenses;
using MyCondo.Domain.Features.Expenses.ExpenseTypes;
using MyCondo.Domain.Features.Finance.ChartOfAccounts;
using MyCondo.Domain.Features.Finance.Funds;
using MyCondo.Domain.Features.Payments.Ledger;
using MyCondo.Domain.Features.Property.Buildings;

namespace MyCondo.Infrastructure.Persistence.Configurations;

public sealed class ExpenseConfiguration : IEntityTypeConfiguration<Expense>
{
    public void Configure(EntityTypeBuilder<Expense> builder)
    {
        builder.ToTable("expenses", schema: "expenses");

        builder.HasKey(x => x.Id).HasName("pk_expenses");
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new ExpenseId(value))
            .ValueGeneratedNever();

        builder.Property(x => x.TenantId).IsRequired();
        // Nullable (Template 3) — an association-wide common expense has no Building.
        builder.Property(x => x.BuildingId)
            .HasConversion(
                id => id == null ? (Guid?)null : id.Value.Value,
                value => value == null ? (BuildingId?)null : new BuildingId(value.Value));
        builder.Property(x => x.ExpenseTypeId)
            .HasConversion(id => id.Value, value => new ExpenseTypeId(value))
            .IsRequired();
        builder.Property(x => x.FundId)
            .HasConversion(
                id => id == null ? (Guid?)null : id.Value.Value,
                value => value == null ? (FundId?)null : new FundId(value.Value));
        builder.Property(x => x.ExpenseDate).IsRequired();
        builder.Property(x => x.AccountingDate).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Payee).HasMaxLength(200);
        builder.Property(x => x.ReferenceNumber).HasMaxLength(100);
        builder.Property(x => x.Amount).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.IsPaid).IsRequired();
        builder.Property(x => x.PaymentMethod).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.Notes).HasMaxLength(1000);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.VoidReason).HasMaxLength(500);
        builder.Property(x => x.FinancialAccountId)
            .HasConversion(
                id => id == null ? (Guid?)null : id.Value.Value,
                value => value == null ? (ChartOfAccountId?)null : new ChartOfAccountId(value.Value));
        builder.Property(x => x.PostingId)
            .HasConversion(
                id => id == null ? (Guid?)null : id.Value.Value,
                value => value == null ? (LedgerPostingId?)null : new LedgerPostingId(value.Value));
        builder.Property(x => x.PaymentPostingId)
            .HasConversion(
                id => id == null ? (Guid?)null : id.Value.Value,
                value => value == null ? (LedgerPostingId?)null : new LedgerPostingId(value.Value));
        builder.Property(x => x.ReversalPostingId)
            .HasConversion(
                id => id == null ? (Guid?)null : id.Value.Value,
                value => value == null ? (LedgerPostingId?)null : new LedgerPostingId(value.Value));
        builder.Property(x => x.PaymentReversalPostingId)
            .HasConversion(
                id => id == null ? (Guid?)null : id.Value.Value,
                value => value == null ? (LedgerPostingId?)null : new LedgerPostingId(value.Value));
        builder.Property(x => x.Version).IsConcurrencyToken();

        builder.HasIndex(x => new { x.TenantId, x.BuildingId })
            .HasDatabaseName("ix_expenses_tenant_id_building_id");

        builder.HasIndex(x => new { x.TenantId, x.ExpenseTypeId })
            .HasDatabaseName("ix_expenses_tenant_id_expense_type_id");

        builder.HasIndex(x => new { x.TenantId, x.ExpenseDate })
            .HasDatabaseName("ix_expenses_tenant_id_expense_date");

        builder.HasIndex(x => new { x.TenantId, x.FundId })
            .HasDatabaseName("ix_expenses_tenant_id_fund_id");

        builder.Ignore(x => x.DomainEvents);
    }
}
