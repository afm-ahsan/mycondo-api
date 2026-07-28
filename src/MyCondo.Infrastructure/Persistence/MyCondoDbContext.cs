using Microsoft.EntityFrameworkCore;
using MyCondo.Domain.Abstractions;

namespace MyCondo.Infrastructure.Persistence;

public sealed class MyCondoDbContext(
    DbContextOptions<MyCondoDbContext> options
) : DbContext(options), IUnitOfWork
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Schema-per-module: each module's IEntityTypeConfiguration<T> sets ToTable(name, schema: "<module>").
        // No HasDefaultSchema() — every aggregate must declare its schema explicitly to avoid silent fallthrough.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MyCondoDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
