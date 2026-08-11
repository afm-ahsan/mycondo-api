using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyCondo.Domain.Features.Expenses.Expenses;
using MyCondo.Domain.Features.Expenses.ExpenseTypes;
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
        builder.Property(x => x.BuildingId)
            .HasConversion(id => id.Value, value => new BuildingId(value))
            .IsRequired();
        builder.Property(x => x.ExpenseTypeId)
            .HasConversion(id => id.Value, value => new ExpenseTypeId(value))
            .IsRequired();
        builder.Property(x => x.ExpenseDate).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Payee).HasMaxLength(200);
        builder.Property(x => x.ReferenceNumber).HasMaxLength(100);
        builder.Property(x => x.Amount).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.PaymentMethod).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.Notes).HasMaxLength(1000);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.VoidReason).HasMaxLength(500);
        builder.Property(x => x.Version).IsConcurrencyToken();

        builder.HasIndex(x => new { x.TenantId, x.BuildingId })
            .HasDatabaseName("ix_expenses_tenant_id_building_id");

        builder.HasIndex(x => new { x.TenantId, x.ExpenseTypeId })
            .HasDatabaseName("ix_expenses_tenant_id_expense_type_id");

        builder.HasIndex(x => new { x.TenantId, x.ExpenseDate })
            .HasDatabaseName("ix_expenses_tenant_id_expense_date");

        builder.Ignore(x => x.DomainEvents);
    }
}
