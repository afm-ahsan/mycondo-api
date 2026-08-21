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

        // Idempotency (ADR-027, template's OrganizationId+SourceType+SourceId+PostingPurpose invariant):
        // ReferenceType already carries the business-event purpose and ReferenceId the source id, so
        // this is a straight uniqueness upgrade of the pre-existing index, not a new concept. Partial
        // (ReferenceId IS NOT NULL) because several existing call sites intentionally post with no
        // natural source id and rely on an upstream domain-level dedup check instead (e.g. batch invoice
        // generation's ExistsForFlatAndPeriodAsync) — unchanged behavior for those.
        builder.HasIndex(x => new { x.TenantId, x.ReferenceType, x.ReferenceId })
            .IsUnique()
            .HasFilter("reference_id IS NOT NULL")
            .HasDatabaseName("ux_ledger_postings_tenant_id_reference_type_reference_id");

        builder.Ignore(x => x.DomainEvents);
    }
}
