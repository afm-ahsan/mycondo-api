using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyCondo.Domain.Features.Platform.PlatformRefreshTokens;
using MyCondo.Domain.Features.Platform.PlatformUsers;

namespace MyCondo.Infrastructure.Persistence.Configurations;

public sealed class PlatformRefreshTokenConfiguration : IEntityTypeConfiguration<PlatformRefreshToken>
{
    public void Configure(EntityTypeBuilder<PlatformRefreshToken> builder)
    {
        builder.ToTable("platform_refresh_tokens", schema: "platform");

        builder.HasKey(x => x.Id).HasName("pk_platform_refresh_tokens");
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new PlatformRefreshTokenId(value))
            .ValueGeneratedNever();

        builder.Property(x => x.PlatformUserId)
            .HasConversion(id => id.Value, value => new PlatformUserId(value));

        builder.Property(x => x.TokenHash).IsRequired().HasMaxLength(128);
        builder.Property(x => x.ExpiresAtUtc).IsRequired();
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.CreatedByIp).IsRequired().HasMaxLength(64);
        builder.Property(x => x.RevokedAtUtc);
        builder.Property(x => x.RevokedByIp).HasMaxLength(64);
        builder.Property(x => x.ReplacedByTokenId)
            .HasConversion(
                id => id.HasValue ? id.Value.Value : (Guid?)null,
                v => v.HasValue ? new PlatformRefreshTokenId(v.Value) : null);

        builder.HasIndex(x => x.TokenHash)
            .IsUnique()
            .HasDatabaseName("ux_platform_refresh_tokens_token_hash");

        builder.HasIndex(x => x.PlatformUserId)
            .HasDatabaseName("ix_platform_refresh_tokens_platform_user_id");
    }
}
