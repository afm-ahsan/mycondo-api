using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyCondo.Domain.Features.Security.AccessSessions;
using MyCondo.Domain.Features.Security.SebaVisits;

namespace MyCondo.Infrastructure.Persistence.Configurations;

public sealed class SebaVisitDetailConfiguration : IEntityTypeConfiguration<SebaVisitDetail>
{
    public void Configure(EntityTypeBuilder<SebaVisitDetail> builder)
    {
        builder.ToTable("seba_visit_details", schema: "security");

        builder.HasKey(x => x.Id).HasName("pk_seba_visit_details");
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new SebaVisitDetailId(value))
            .ValueGeneratedNever();

        builder.Property(x => x.TenantId).IsRequired();
        builder.Property(x => x.AccessSessionId)
            .HasConversion(id => id.Value, value => new AccessSessionId(value))
            .IsRequired();
        builder.Property(x => x.VisitorFullName).IsRequired().HasMaxLength(200);
        builder.Property(x => x.VisitorPhone).HasMaxLength(20);
        builder.Property(x => x.Organization).HasMaxLength(200);
        builder.Property(x => x.DepartmentOrEmployeeToMeet).HasMaxLength(200);
        builder.Property(x => x.TokenNumber).HasMaxLength(40);
        builder.Property(x => x.RelatedReferenceType).HasMaxLength(40);
        builder.Property(x => x.RelatedReferenceId);
        builder.Property(x => x.ServiceOutcome).HasMaxLength(500);
        builder.Property(x => x.Acknowledged).IsRequired();
        builder.Property(x => x.Version).IsConcurrencyToken();

        builder.HasIndex(x => x.AccessSessionId)
            .IsUnique()
            .HasDatabaseName("ux_seba_visit_details_access_session_id");

        builder.Ignore(x => x.DomainEvents);
    }
}
