using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyCondo.Domain.Features.Payments.Ledger;

namespace MyCondo.Infrastructure.Persistence.Configurations;

public sealed class LedgerPostingConfiguration : IEntityTypeConfiguration<LedgerPosting>
{
    public void Configure(EntityTypeBuilder<LedgerPosting> builder)
    {
        builder.ToTable("ledger_postings", schema: "payments");

        builder.HasKey(x => x.Id).HasName("pk_ledger_postings");
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new LedgerPostingId(value))
            .ValueGeneratedNever();

        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.BusinessDate).IsRequired();
        builder.Property(x => x.Description).IsRequired().HasMaxLength(500);
        builder.Property(x => x.ReferenceType).HasMaxLength(100);
        builder.Property(x => x.ReferenceId);
        builder.Property(x => x.PostedAtUtc).IsRequired();

        builder.HasIndex(x => new { x.TenantId, x.BusinessDate })
            .HasDatabaseName("ix_ledger_postings_tenant_id_business_date");

        builder.HasIndex(x => new { x.TenantId, x.ReferenceType, x.ReferenceId })
            .HasDatabaseName("ix_ledger_postings_tenant_id_reference_type_reference_id");

        builder.Ignore(x => x.DomainEvents);
    }
}
