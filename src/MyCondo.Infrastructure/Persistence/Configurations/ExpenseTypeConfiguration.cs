using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyCondo.Domain.Features.Expenses.ExpenseCategories;
using MyCondo.Domain.Features.Expenses.ExpenseTypes;

namespace MyCondo.Infrastructure.Persistence.Configurations;

public sealed class ExpenseTypeConfiguration : IEntityTypeConfiguration<ExpenseType>
{
    public void Configure(EntityTypeBuilder<ExpenseType> builder)
    {
        builder.ToTable("expense_types", schema: "expenses");

        builder.HasKey(x => x.Id).HasName("pk_expense_types");
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new ExpenseTypeId(value))
            .ValueGeneratedNever();

        builder.Property(x => x.TenantId).IsRequired();
        // Nullable at storage level only to accommodate rows created before Template 3 — see
        // ExpenseType's doc comment.
        builder.Property(x => x.ExpenseCategoryId)
            .HasConversion(
                id => id == null ? (Guid?)null : id.Value.Value,
                value => value == null ? (ExpenseCategoryId?)null : new ExpenseCategoryId(value.Value));
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Code).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.DisplayOrder).IsRequired();
        builder.Property(x => x.Version).IsConcurrencyToken();

        builder.HasIndex(x => new { x.TenantId, x.Code })
            .IsUnique()
            .HasDatabaseName("ux_expense_types_tenant_id_code");

        builder.HasIndex(x => new { x.TenantId, x.Name })
            .IsUnique()
            .HasDatabaseName("ux_expense_types_tenant_id_name");

        builder.HasIndex(x => new { x.TenantId, x.ExpenseCategoryId })
            .HasDatabaseName("ix_expense_types_tenant_id_expense_category_id");

        builder.Ignore(x => x.DomainEvents);
    }
}
