using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyCondo.Domain.Features.Operations.Generators;
using MyCondo.Domain.Features.Operations.GeneratorSessions;

namespace MyCondo.Infrastructure.Persistence.Configurations;

public sealed class GeneratorSessionConfiguration : IEntityTypeConfiguration<GeneratorSession>
{
    public void Configure(EntityTypeBuilder<GeneratorSession> builder)
    {
        builder.ToTable("generator_sessions", schema: "operations");

        builder.HasKey(x => x.Id).HasName("pk_generator_sessions");
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new GeneratorSessionId(value))
            .ValueGeneratedNever();

        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.GeneratorId)
            .HasConversion(id => id.Value, value => new GeneratorId(value))
            .IsRequired();
        builder.Property(x => x.StartAtUtc).IsRequired();
        builder.Property(x => x.StopAtUtc);
        builder.Property(x => x.OperatorId);
        builder.Property(x => x.OpeningFuelLevel).HasPrecision(10, 2).IsRequired();
        builder.Property(x => x.ClosingFuelLevel).HasPrecision(10, 2);
        builder.Property(x => x.OutageReason).HasMaxLength(500);
        builder.Property(x => x.RuntimeMinutes);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(10).IsRequired();

        builder.HasIndex(x => new { x.TenantId, x.GeneratorId, x.Status })
            .HasDatabaseName("ix_generator_sessions_tenant_id_generator_id_status");

        builder.Ignore(x => x.DomainEvents);
    }
}
