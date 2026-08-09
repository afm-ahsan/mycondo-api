# EF Core 10 Configurations and Migrations

This document covers DbContext setup, entity configurations, and migration practices.

---

## 1. DbContext

```csharp
public sealed class AppDbContext(
    DbContextOptions<AppDbContext> options,
    IDispatchDomainEvents dispatcher
) : DbContext(options), IUnitOfWork
{
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Quotation> Quotations => Set<Quotation>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    // ... one DbSet per aggregate root

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.HasDefaultSchema("app");
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(builder);
    }
}
```

### Rules

- **`sealed`.**
- **One `DbContext` per bounded context.**
- **`DbSet<T>` for aggregate roots only.** Children are reached through aggregates.
- **`HasDefaultSchema("app")`** for business tables. Other schemas set per configuration.
- **`ApplyConfigurationsFromAssembly`** picks up all `IEntityTypeConfiguration<T>` files automatically.
- **No inline `modelBuilder.Entity<T>()`** in `OnModelCreating`.

---

## 2. DbContext Registration

```csharp
services.AddDbContext<AppDbContext>((sp, options) =>
{
    options
        .UseNpgsql(
            configuration.GetConnectionString("Default"),
            npg => npg.MigrationsHistoryTable("__ef_migrations_history", "app"))
        .UseSnakeCaseNamingConvention()
        .AddInterceptors(
            sp.GetRequiredService<AuditInterceptor>(),
            sp.GetRequiredService<DispatchDomainEventsInterceptor>(),
            sp.GetRequiredService<SoftDeleteInterceptor>());

    if (env.IsDevelopment())
        options.EnableSensitiveDataLogging().EnableDetailedErrors();
});

services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AppDbContext>());

// NpgsqlDataSource singleton for Dapper read-side
services.AddSingleton<NpgsqlDataSource>(_ =>
    NpgsqlDataSource.Create(configuration.GetConnectionString("Default")!));
```

### Rules

- **Migrations history table** lives in the same schema as business tables (`app.__ef_migrations_history`) so the schema is self-contained.
- **`UseSnakeCaseNamingConvention()`** — non-negotiable.
- **Interceptors registered as scoped** services so they get fresh dependencies per request.
- **Sensitive data logging only in development.**

---

## 3. Entity Configurations

One file per entity, in `Persistence/Configurations/`:

```csharp
public sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("customer", schema: "app");
        builder.HasKey(x => x.Id).HasName("pk_customer");

        // Strongly-typed ID
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new CustomerId(value))
            .ValueGeneratedNever();

        // Required strings
        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(200);

        // Owned value object — single property
        builder.OwnsOne(x => x.Email, email =>
        {
            email.Property(e => e.Value)
                 .HasColumnName("email")
                 .IsRequired()
                 .HasMaxLength(320);
            email.HasIndex(e => e.Value)
                 .IsUnique()
                 .HasDatabaseName("uix_customer_email");
        });

        // Smart enum / state — store as text
        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(40)
            .IsRequired();

        // Concurrency token
        builder.Property(x => x.Version).IsConcurrencyToken();

        // Audit columns (also handled by AuditInterceptor)
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
        builder.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc").IsRequired();
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by");

        // Soft delete
        builder.Property(x => x.DeletedAtUtc).HasColumnName("deleted_at_utc");
        builder.HasQueryFilter(x => x.DeletedAtUtc == null);

        // Owned collection (child entity)
        builder.OwnsMany(x => x.ContactPersons, cp =>
        {
            cp.ToTable("customer_contact_person", schema: "app");
            cp.WithOwner().HasForeignKey("customer_id");
            cp.HasKey(x => x.Id).HasName("pk_customer_contact_person");

            cp.Property(x => x.Id)
              .HasConversion(id => id.Value, value => new ContactPersonId(value));

            cp.Property(x => x.Name).IsRequired().HasMaxLength(200);
            cp.Property(x => x.PhoneNumber).IsRequired().HasMaxLength(50);
            cp.Property(x => x.IsPrimary).IsRequired();
        });

        // Compound index
        builder.HasIndex(x => new { x.Status, x.CreatedAtUtc })
               .HasDatabaseName("ix_customer_status_created");

        // Domain events — never persisted
        builder.Ignore(x => x.DomainEvents);
    }
}
```

### Rules

- **`sealed`.**
- **Explicit constraint names** — `pk_*`, `fk_*`, `uix_*`, `ix_*`. EF auto-names are forbidden.
- **`HasConversion`** for strongly-typed IDs.
- **`OwnsOne` / `OwnsMany`** for value objects and child entities.
- **`HasMaxLength`** on every string column. Leaving it off creates `text` (which is fine for some, but most strings have a real limit).
- **`HasQueryFilter`** for soft delete — applied automatically to all queries.
- **`Ignore(x => x.DomainEvents)`** so domain events aren't persisted.

---

## 4. Configuring Foreign Keys

When two aggregates relate, use a **reference by ID**, not a navigation property:

```csharp
public sealed class Invoice : AggregateRoot<InvoiceId>
{
    public CustomerId CustomerId { get; private set; }
    // ... NOT public Customer Customer { get; private set; }
}

public sealed class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.Property(x => x.CustomerId)
            .HasConversion(id => id.Value, value => new CustomerId(value))
            .HasColumnName("customer_id");

        builder.HasIndex(x => x.CustomerId)
               .HasDatabaseName("ix_invoice_customer");

        // No HasOne(x => x.Customer) — keeps aggregates loosely coupled.
    }
}
```

For cross-aggregate queries, project on the read side via Dapper:

```csharp
const string sql = """
    SELECT i.*, c.name AS customer_name
    FROM app.invoice i
    JOIN app.customer c ON c.id = i.customer_id
    WHERE i.id = @Id;
    """;
```

### Rules

- **Aggregate boundaries are reflected at the FK level.** Don't add a navigation property between aggregates — it tempts developers to load entire object graphs.
- **Foreign keys are reference-by-ID only.** Navigations are allowed *within* an aggregate (parent → children).
- **Joins for read** happen in Dapper / read repositories, not in the write-side aggregate.

---

## 5. Migrations

### Add a migration

```bash
dotnet ef migrations add Add_Customer_Table \
    --project src/<Project>.Infrastructure \
    --startup-project src/<Project>.Api \
    --output-dir Persistence/Migrations
```

### Migration file structure

```csharp
/// <summary>
/// Adds the customer aggregate. Initial business table.
///
/// Why: First module of the system, foundational for invoicing and AMC.
/// Notes: No data backfill required (greenfield).
/// </summary>
public partial class Add_Customer_Table : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema("app");
        migrationBuilder.CreateTable(
            name: "customer",
            schema: "app",
            columns: table => new
            {
                id = table.Column<Guid>(nullable: false),
                name = table.Column<string>(maxLength: 200, nullable: false),
                email = table.Column<string>(maxLength: 320, nullable: false),
                status = table.Column<string>(maxLength: 40, nullable: false),
                created_at_utc = table.Column<DateTimeOffset>(nullable: false),
                updated_at_utc = table.Column<DateTimeOffset>(nullable: false),
                created_by = table.Column<Guid>(nullable: true),
                updated_by = table.Column<Guid>(nullable: true),
                deleted_at_utc = table.Column<DateTimeOffset>(nullable: true),
                version = table.Column<int>(nullable: false, defaultValue: 1)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_customer", x => x.id);
            });

        migrationBuilder.CreateIndex(
            name: "uix_customer_email",
            schema: "app",
            table: "customer",
            column: "email",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_customer_status_created",
            schema: "app",
            table: "customer",
            columns: new[] { "status", "created_at_utc" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "customer", schema: "app");
    }
}
```

### Rules

- **Descriptive names**: `Add_Customer_Table`, `Add_Quotation_Status_Index`, `Backfill_Customer_DefaultRegion`. Never `Migration1` or `Update`.
- **Comment block at the top** describes intent (why, not what).
- **One concern per migration.** Don't bundle "add table + add index + backfill" in one file.
- **`Down()` is implemented**, even if "drop". A migration that can't be rolled back is a risk.
- **Migrations are reviewed** on PR — destructive changes require a second engineer's approval.

---

## 6. Expand → Migrate → Contract

For breaking schema changes (e.g. renaming a column), use three deploys:

### Phase 1: Expand
- Add the new column **without** dropping the old one.
- Update app code to **write to both**.

### Phase 2: Migrate
- Backfill data from old → new.
- Switch app code to **read from new**.
- Verify in production.

### Phase 3: Contract
- Stop writing to old.
- Drop the old column.

### Rules

- **Never `DROP COLUMN` in the same deploy** as code change that stops using it. Production traffic from the old container will fail.
- **`ALTER COLUMN TYPE` is destructive** — apply on a new column with backfill, then swap.
- **Document each phase as its own migration** with explicit names: `Expand_Customer_Add_FullName`, `Backfill_Customer_FullName`, `Contract_Customer_Drop_DisplayName`.

---

## 7. Seed Data

Seed data is for **bootstrap data the app cannot start without**: roles, permissions, system users,
default lookups. It does **not** belong in EF Core migrations (no `InsertData`/`HasData` for evolving
catalogues — see MyCondo's own migration history, `Seed_Permission_Catalogue` and its successors, which
did this and were superseded; those files are preserved as historical record, not as a pattern to
repeat). Migrations define schema and genuine migration-time *data transformations* on existing
production rows; seed data is application-owned reference/catalogue data, reconciled by a dedicated
seeder.

**Do not gate a seeder with `if (await db.Roles.AnyAsync(ct)) return;`.** That pattern — checked once,
short-circuits forever — silently prevents a role or permission added to the catalogue *after* the
first successful run from ever reaching an already-bootstrapped environment. It looks idempotent
(rerunning it never crashes) but it is not idempotent in the sense that matters: idempotent means
*running it produces the same end state regardless of how many times it's run*, not *running it twice
is a no-op*.

Instead, reconcile by a **stable natural key** (a permission's `Name`, a role's `Code`, a
configuration's `Key` — never a database-generated ID):

```csharp
public static class PermissionSeeder
{
    private static readonly (string Name, string Description, string Module)[] Catalogue =
    [
        ("role.manage", "Create roles and manage grants/assignments", "role"),
        // ...
    ];

    public static async Task SeedAsync(IPermissionRepository permissions, CancellationToken ct)
    {
        HashSet<string> existingNames = (await permissions.GetAllAsync(ct))
            .Select(p => p.Name).ToHashSet();

        foreach (var (name, description, module) in Catalogue)
        {
            if (existingNames.Contains(name)) continue; // never update/remove an existing row here
            permissions.Add(Permission.Create(PermissionId.New(), name, description, module));
        }
    }
}
```

Called explicitly, in an auditable order, from one orchestration entry point — not scattered across
several unrelated `IHostedService` registrations:

```csharp
public static async Task SeedDatabaseAsync(this IServiceProvider services, IHostEnvironment env, CancellationToken ct)
{
    using IServiceScope scope = services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<IPermissionSeeder>().SeedAsync(ct);
    await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync(ct);

    if (env.IsDevelopment()) // hard runtime guard, not just DI-registration gating
    {
        // Development/demo-only seeders here.
    }
}
```

### Rules

- **Reconcile by stable natural key, not `AnyAsync`-then-return.** Create missing entries; never
  auto-delete an entry the source catalogue no longer lists (treat that as a deliberate deprecation
  decision, not something a seeder does silently).
- **Idempotent means the end state is correct after any number of runs**, including a run that happens
  after new catalogue entries were added — not merely "running it twice doesn't crash."
- **Separate concerns**: system catalogue (permissions, roles, mandatory lookups — required in every
  environment) vs. authentication/platform bootstrap (a SuperAdmin identity — a true singleton, fine to
  guard with an existence check since there's no catalogue to drift) vs. development/demo data (must
  carry a hard runtime environment guard, not just conditional DI registration).
- **No fake business data in seeds for production.** Use a separate, clearly-named Development-only
  seeder for local dev/demo data, and guard it at the call site with `IHostEnvironment.IsDevelopment()`
  in addition to whatever DI wiring exists — never rely on DI registration alone to keep it out of
  Production.
- **Tenant-scoped catalogue seeders need an explicit tenant context**, not the request-bound one — see
  the multi-tenancy/RLS conventions doc for the pattern (a small, fixed `ITenantContextAccessor`
  implementation bound to the tenant being seeded, wired into its own short-lived `DbContext`). Global,
  tenant-less catalogue tables (no `tenant_id`, no RLS policy) need no such accessor at all.
- **Guard concurrent startup.** If multiple app instances can start at once, wrap the seed sequence in
  a Postgres advisory lock (`pg_advisory_lock`/`pg_advisory_unlock`) rather than assuming a single
  process — every seeder should already be safe under that lock because it reconciles by natural key,
  but the lock avoids redundant work and races.
- **Permissions and roles are seed data.** Customers, invoices, products are not.

---

## 8. Production Migration Strategy

```bash
# Generate idempotent SQL once per release, store in deployment artifacts:
dotnet ef migrations script --idempotent \
    --project src/<Project>.Infrastructure \
    --startup-project src/<Project>.Api \
    --output ./scripts/migration.sql
```

Apply via:
- **A short-lived migration job** in CI/CD (recommended).
- **Application startup** for very small projects (acceptable but takes the app down for the migration window).

### Rules

- **Migrations and code deploy separately.** Migration runs first, then the new code.
- **Idempotent script** (the `--idempotent` flag) is safe to re-apply.
- **Backup before destructive changes.** Run `pg_dump` before any migration that drops/alters columns.
- **Never run `dotnet ef database update` directly against production.**

---

## 9. Testing Migrations

Integration tests apply migrations to the Testcontainer-Postgres on startup. This catches:
- Migration syntax errors.
- Constraint violations against existing data (when seed data is added).
- Schema-vs-EF-model drift.

```csharp
public async ValueTask InitializeAsync()
{
    await _container.StartAsync();
    using var scope = Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}
```

---

## 10. Schema Drift Detection

Add a CI check:

```bash
dotnet ef migrations has-pending-model-changes \
  --project src/<Project>.Infrastructure \
  --startup-project src/<Project>.Api
# Exit code 0 = no pending; non-zero = drift detected.
```

### Rules

- **Run on every PR.** Catches the case where someone modifies an entity but forgets to add a migration.
- **A drift in CI fails the build.**

---

## 11. Common Mistakes

| Mistake                                                          | Fix                                                              |
|------------------------------------------------------------------|------------------------------------------------------------------|
| Inline `modelBuilder.Entity<T>(...)` in `OnModelCreating`        | One `IEntityTypeConfiguration<T>` per entity                     |
| EF auto-generated constraint names                               | Explicit `pk_*`, `fk_*`, `uix_*`, `ix_*`                         |
| Cross-aggregate navigation properties                            | Reference by ID + Dapper joins for reads                         |
| Bundling unrelated changes in one migration                      | One concern per migration                                        |
| Renaming a column directly                                       | Expand → Migrate → Contract                                      |
| `dotnet ef database update` against production                   | Use idempotent script in CI/CD                                   |
| Forgetting `HasMaxLength`                                        | Most strings have a real business limit                          |
| Lazy-loading entire aggregate trees on read                      | Use Dapper for reads                                             |
| Seed data with fake business records                             | Only system/foundation data; use dev-seed for local              |
| `Down()` empty                                                   | Implement reverse, even if a `DROP`                              |
| `EF Core InsertData`/`HasData` for an evolving catalogue         | Dedicated seeder reconciling by natural key, outside migrations   |
| Seeder gated by `if (await db.X.AnyAsync()) return;`              | Reconcile by natural key — create missing, never touch the rest  |
| Development/demo seeder guarded only by DI registration           | Add a hard runtime `IHostEnvironment.IsDevelopment()` check too  |
