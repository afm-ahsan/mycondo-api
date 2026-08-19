using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyCondo.Domain.Features.Finance.AccountMappings;
using MyCondo.Domain.Features.Finance.ChartOfAccounts;

namespace MyCondo.Infrastructure.Persistence.Configurations;

public sealed class AccountMappingConfiguration : IEntityTypeConfiguration<AccountMapping>
{
    public void Configure(EntityTypeBuilder<AccountMapping> builder)
    {
        builder.ToTable("account_mappings", schema: "finance");

        builder.HasKey(x => x.Id).HasName("pk_account_mappings");
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new AccountMappingId(value))
            .ValueGeneratedNever();

        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.PostingRole).IsRequired().HasMaxLength(100);

        builder.Property(x => x.ChartOfAccountId)
            .HasConversion(id => id.Value, value => new ChartOfAccountId(value))
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.CreatedBy);
        builder.Property(x => x.UpdatedAtUtc);
        builder.Property(x => x.UpdatedBy);

        builder.HasIndex(x => new { x.TenantId, x.PostingRole })
            .IsUnique()
            .HasDatabaseName("ux_account_mappings_tenant_id_posting_role");

        builder.Ignore(x => x.DomainEvents);
    }
}
