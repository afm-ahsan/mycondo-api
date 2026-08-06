using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyCondo.Domain.Features.Payments.Ledger;
using MyCondo.Domain.Features.Property.Flats;

namespace MyCondo.Infrastructure.Persistence.Configurations;

public sealed class LedgerEntryConfiguration : IEntityTypeConfiguration<LedgerEntry>
{
    public void Configure(EntityTypeBuilder<LedgerEntry> builder)
    {
        builder.ToTable("ledger_entries", schema: "payments");

        builder.HasKey(x => x.Id).HasName("pk_ledger_entries");
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new LedgerEntryId(value))
            .ValueGeneratedNever();

        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.PostingId)
            .HasConversion(id => id.Value, value => new LedgerPostingId(value))
            .IsRequired();

        builder.Property(x => x.AccountType).HasConversion<string>().HasMaxLength(30).IsRequired();

        builder.Property(x => x.FlatId)
            .HasConversion(
                id => id.HasValue ? id.Value.Value : (Guid?)null,
                value => value.HasValue ? new FlatId(value.Value) : (FlatId?)null);

        builder.Property(x => x.Direction).HasConversion<string>().HasMaxLength(10).IsRequired();
        builder.Property(x => x.Amount).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.BusinessDate).IsRequired();
        builder.Property(x => x.Description).IsRequired().HasMaxLength(500);
        builder.Property(x => x.CreatedAtUtc).IsRequired();

        builder.HasIndex(x => new { x.TenantId, x.PostingId })
            .HasDatabaseName("ix_ledger_entries_tenant_id_posting_id");

        builder.HasIndex(x => new { x.TenantId, x.FlatId, x.AccountType })
            .HasDatabaseName("ix_ledger_entries_tenant_id_flat_id_account_type");
    }
}
