# Infrastructure Layer Conventions

The Infrastructure layer adapts the outside world to the abstractions Domain and Application define. It owns EF Core, Redis, message brokers, file storage, and 3rd-party HTTP clients.

> Replace `<Aggregate>` with the aggregate name (`Customer`).

---

## 1. Allowed Dependencies

The `Infrastructure` `.csproj` may reference:
- `<Project>.Domain`, `<Project>.Application`, `<Project>.Shared`.
- `Microsoft.EntityFrameworkCore`, `Npgsql.EntityFrameworkCore.PostgreSQL`, `EFCore.NamingConventions`.
- `Dapper`, `StackExchange.Redis`.
- `Microsoft.AspNetCore.Identity.EntityFrameworkCore` (when using ASP.NET Identity for users).
- `BCrypt.Net-Next`.
- Messaging libraries (RabbitMQ.Client, MassTransit, Azure.Messaging.ServiceBus).
- 3rd-party SDKs the project actually integrates with.

Forbidden:
- `Microsoft.AspNetCore.Mvc.*` — Infrastructure is HTTP-agnostic.
- Test frameworks (xUnit, NSubstitute) — only test projects reference those.

---

## 2. AppDbContext

```csharp
public sealed class AppDbContext(
    DbContextOptions<AppDbContext> options,
    IDispatchDomainEvents dispatcher
) : DbContext(options), IUnitOfWork
{
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Quotation> Quotations => Set<Quotation>();
    // ... one DbSet per aggregate root

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.HasDefaultSchema("app");
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(builder);
    }

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        // Audit + domain event dispatch happens via interceptors registered in DI.
        return await base.SaveChangesAsync(ct);
    }
}
```

### Rules

- **`sealed`.**
- **One `DbContext` per bounded context.** No god-context.
- **`DbSet<T>` only for aggregate roots.** Child entities are reached via the aggregate.
- **`DbSet<T>` getters are `=> Set<T>()`** — primary constructor's `DbContextOptions` parameter prevents the auto-generated default.
- **Schema set explicitly** via `HasDefaultSchema(...)`. Default to `app` for business tables.
- **Configurations applied from assembly** — never inline `modelBuilder.Entity<T>()` calls.

---

## 3. Entity Configurations

One `IEntityTypeConfiguration<T>` per entity, in `Persistence/Configurations/`:

```csharp
public sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("customer", schema: "app");

        builder.HasKey(x => x.Id).HasName("pk_customer");
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => new CustomerId(value))
            .ValueGeneratedNever();

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.OwnsOne(x => x.Email, email =>
        {
            email.Property(e => e.Value)
                 .HasColumnName("email")
                 .IsRequired()
                 .HasMaxLength(320);
            email.HasIndex(e => e.Value).IsUnique().HasDatabaseName("uix_customer_email");
        });

        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(40);

        builder.Property(x => x.Version).IsConcurrencyToken();

        // Audit columns — handled by AuditInterceptor at runtime
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        builder.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");

        // Soft delete query filter
        builder.HasQueryFilter(x => x.DeletedAtUtc == null);

        // Child entity
        builder.OwnsMany(x => x.ContactPersons, cp =>
        {
            cp.ToTable("customer_contact_person", schema: "app");
            cp.WithOwner().HasForeignKey("customer_id");
            cp.HasKey(x => x.Id);
        });

        // Domain events are not persisted
        builder.Ignore(x => x.DomainEvents);
    }
}
```

### Rules

- **`sealed`.**
- **Explicit constraint names** — `pk_customer`, `uix_customer_email`. EF auto-names are forbidden.
- **Strongly-typed ID conversions** via `HasConversion(id => id.Value, value => new TId(value))`.
- **Value objects** mapped via `OwnsOne` / `OwnsMany`.
- **Concurrency token** on the `Version` column with `IsConcurrencyToken()`.
- **Soft-delete query filter** when the entity supports soft delete.
- **`Ignore(x => x.DomainEvents)`** — events are never persisted.

---

## 4. DbContext Configuration in DI

```csharp
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<IClock, SystemClock>();
        services.AddScoped<ICurrentUserProvider, CurrentUserProvider>();

        services.AddScoped<AuditInterceptor>();
        services.AddScoped<DispatchDomainEventsInterceptor>();
        services.AddScoped<SoftDeleteInterceptor>();

        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            options.UseNpgsql(
                configuration.GetConnectionString("Default"),
                npg => npg.MigrationsHistoryTable("__ef_migrations_history", "app"))
                .UseSnakeCaseNamingConvention()
                .AddInterceptors(
                    sp.GetRequiredService<AuditInterceptor>(),
                    sp.GetRequiredService<DispatchDomainEventsInterceptor>(),
                    sp.GetRequiredService<SoftDeleteInterceptor>());

            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
                options.EnableSensitiveDataLogging().EnableDetailedErrors();
        });

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AppDbContext>());

        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<ICustomerReadRepository, CustomerReadRepository>();

        services.AddSingleton<IConnectionMultiplexer>(_ =>
            ConnectionMultiplexer.Connect(configuration.GetConnectionString("Redis")!));
        services.AddSingleton<ICacheService, RedisCacheService>();
        services.AddSingleton<IDistributedLockProvider, RedisDistributedLockProvider>();

        return services;
    }
}
```

---

## 5. Write-Side Repositories

```csharp
public sealed class CustomerRepository(AppDbContext db) : ICustomerRepository
{
    public Task<Customer?> GetByIdAsync(CustomerId id, CancellationToken ct) =>
        db.Customers
          .Include(c => c.ContactPersons)
          .FirstOrDefaultAsync(c => c.Id == id, ct);

    public Task<Customer?> GetByEmailAsync(Email email, CancellationToken ct) =>
        db.Customers
          .Include(c => c.ContactPersons)
          .FirstOrDefaultAsync(c => c.Email.Value == email.Value, ct);

    public void Add(Customer customer) => db.Customers.Add(customer);
    public void Remove(Customer customer) => db.Customers.Remove(customer);
}
```

### Rules

- **Repositories are scoped** — same lifetime as `DbContext`.
- **No `IQueryable<T>` exposed.** Materialize before returning.
- **`Add` / `Remove` are sync** — they only touch the change tracker.
- **`Include` what's needed for the aggregate's invariants.** Don't `Include` for read use cases — those go through the read repository.

---

## 6. Read-Side Repositories (Dapper)

For complex queries, projections, joins across aggregates, and high-volume reads, use Dapper.

```csharp
public sealed class CustomerReadRepository(NpgsqlDataSource dataSource) : ICustomerReadRepository
{
    public async Task<CustomerDetailDto?> GetByIdAsync(CustomerId id, CancellationToken ct)
    {
        const string sql = """
            SELECT
                c.id            AS Id,
                c.name          AS Name,
                c.email         AS Email,
                c.status        AS Status,
                c.created_at_utc AS CreatedAtUtc,
                c.updated_at_utc AS UpdatedAtUtc
            FROM app.customer c
            WHERE c.id = @Id AND c.deleted_at_utc IS NULL;
            """;

        await using var conn = await dataSource.OpenConnectionAsync(ct);
        var dto = await conn.QuerySingleOrDefaultAsync<CustomerDetailDto>(
            new CommandDefinition(sql, new { Id = id.Value }, cancellationToken: ct));

        return dto;
    }

    public async Task<PagedList<CustomerSummaryDto>> SearchAsync(
        string? search, int page, int pageSize, CancellationToken ct)
    {
        const string sql = """
            SELECT id, name, email, status, created_at_utc
            FROM app.customer
            WHERE deleted_at_utc IS NULL
              AND (@Search IS NULL OR name ILIKE '%' || @Search || '%' OR email ILIKE '%' || @Search || '%')
            ORDER BY created_at_utc DESC
            OFFSET @Offset LIMIT @Limit;
            """;

        const string countSql = """
            SELECT COUNT(*)
            FROM app.customer
            WHERE deleted_at_utc IS NULL
              AND (@Search IS NULL OR name ILIKE '%' || @Search || '%' OR email ILIKE '%' || @Search || '%');
            """;

        await using var conn = await dataSource.OpenConnectionAsync(ct);
        var offset = (page - 1) * pageSize;
        var args = new { Search = search, Offset = offset, Limit = pageSize };

        var items = (await conn.QueryAsync<CustomerSummaryDto>(
            new CommandDefinition(sql, args, cancellationToken: ct))).ToList();
        var total = await conn.ExecuteScalarAsync<long>(
            new CommandDefinition(countSql, args, cancellationToken: ct));

        return new PagedList<CustomerSummaryDto>(items, page, pageSize, total);
    }
}
```

### Rules

- **Use `NpgsqlDataSource`** (registered as singleton) for connection pooling.
- **Parameterize everything.** Dapper does this for you; never use string interpolation.
- **Raw SQL in `""" ... """`** raw string literals — readable and SQL-syntax-friendly in IDEs.
- **Larger SQL files** live in `Persistence/ReadModels/Sql/<query>.sql` and are loaded via `EmbeddedResource`.
- **Read DTOs are different from write DTOs** sometimes — that's fine. Optimize for the query.

---

## 7. Interceptors

### `AuditInterceptor`

Sets `CreatedAtUtc`, `UpdatedAtUtc`, `CreatedBy`, `UpdatedBy` on save:

```csharp
public sealed class AuditInterceptor(
    IClock clock,
    ICurrentUserProvider currentUser
) : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken ct = default)
    {
        if (eventData.Context is null) return base.SavingChangesAsync(eventData, result, ct);

        var nowUtc = clock.UtcNow;
        var userId = currentUser.UserId?.Value;

        foreach (var entry in eventData.Context.ChangeTracker.Entries<IAuditable>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAtUtc = nowUtc;
                entry.Entity.CreatedBy = userId;
            }
            if (entry.State is EntityState.Added or EntityState.Modified)
            {
                entry.Entity.UpdatedAtUtc = nowUtc;
                entry.Entity.UpdatedBy = userId;
            }
        }

        return base.SavingChangesAsync(eventData, result, ct);
    }
}
```

### `SoftDeleteInterceptor`

Translates `Remove(x)` calls into setting `DeletedAtUtc`:

```csharp
public sealed class SoftDeleteInterceptor(IClock clock) : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken ct = default)
    {
        if (eventData.Context is null) return base.SavingChangesAsync(eventData, result, ct);

        foreach (var entry in eventData.Context.ChangeTracker.Entries<ISoftDeletable>())
        {
            if (entry.State == EntityState.Deleted)
            {
                entry.State = EntityState.Modified;
                entry.Entity.DeletedAtUtc = clock.UtcNow;
            }
        }

        return base.SavingChangesAsync(eventData, result, ct);
    }
}
```

### `DispatchDomainEventsInterceptor`

After save, dispatches domain events through MediatR:

```csharp
public sealed class DispatchDomainEventsInterceptor(IPublisher publisher) : SaveChangesInterceptor
{
    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken ct = default)
    {
        if (eventData.Context is null) return result;

        var aggregates = eventData.Context.ChangeTracker
            .Entries<IAggregateRoot>()
            .Where(e => e.Entity.DomainEvents.Count > 0)
            .Select(e => e.Entity)
            .ToList();

        var events = aggregates.SelectMany(a => a.DomainEvents).ToList();
        foreach (var aggregate in aggregates) aggregate.ClearDomainEvents();

        foreach (var domainEvent in events)
            await publisher.Publish(domainEvent, ct);

        return result;
    }
}
```

### Rules

- **Domain events dispatch *after* save.** If the save fails, no events fire.
- **Audit and soft-delete fire *before* save.** They mutate the entity that's being persisted.
- **Idempotent integration events** flow through the **outbox** pattern — written in the same transaction as the data, dispatched asynchronously by `OutboxProcessor`.

---

## 8. Caching (Redis)

```csharp
public sealed class RedisCacheService(
    IConnectionMultiplexer connection,
    ILogger<RedisCacheService> logger
) : ICacheService
{
    private readonly IDatabase _db = connection.GetDatabase();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct)
    {
        var raw = await _db.StringGetAsync(key);
        if (raw.IsNullOrEmpty) return default;
        try
        {
            return JsonSerializer.Deserialize<T>(raw!, JsonOptions);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Failed to deserialize cache value for {Key}", key);
            await _db.KeyDeleteAsync(key);
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(value, JsonOptions);
        await _db.StringSetAsync(key, json, ttl);
    }

    public Task RemoveAsync(string key, CancellationToken ct) =>
        _db.KeyDeleteAsync(key);
}
```

### Rules

- **Cache keys** follow `<aggregate>:<id>` or `<scope>:<id>:<purpose>` (see `01-backend/06-coding-standards.md`).
- **TTLs are explicit** — there is no "default cache forever".
- **Invalidate on write.** When an aggregate changes, remove its cache key inside the handler (or via a domain-event handler).
- **Don't cache write-side data.** Cache projected DTOs only.

---

## 9. External Clients

For 3rd-party HTTP services (payment, email, SMS):

```csharp
public sealed class StripeClient(HttpClient http, IOptions<StripeSettings> options) : IPaymentService
{
    public async Task<PaymentResult> ChargeAsync(
        ChargeRequest request, CancellationToken ct)
    {
        var resp = await http.PostAsJsonAsync("/v1/charges", request, ct);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<PaymentResult>(cancellationToken: ct))!;
    }
}
```

Registered with `AddHttpClient<IPaymentService, StripeClient>` and configured with Polly for retries / circuit breaker:

```csharp
services.AddHttpClient<IPaymentService, StripeClient>(client =>
{
    client.BaseAddress = new Uri(configuration["Stripe:BaseUrl"]!);
    client.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue("Bearer", configuration["Stripe:ApiKey"]);
})
.AddStandardResilienceHandler();
```

### Rules

- **One typed client per external service.**
- **Polly resilience handler** on every external call — retries, circuit breaker, timeout.
- **Settings via `IOptions<T>`** with validation.
- **No retries on idempotency-sensitive operations** unless the external API supports an idempotency key.

---

## 10. Migrations

```bash
# Add a migration
dotnet ef migrations add Add_Customer_Table \
    --project src/<Project>.Infrastructure \
    --startup-project src/<Project>.Api \
    --output-dir Persistence/Migrations

# Apply migrations to the local DB
dotnet ef database update \
    --project src/<Project>.Infrastructure \
    --startup-project src/<Project>.Api

# Generate idempotent SQL script for prod deployment
dotnet ef migrations script --idempotent \
    --project src/<Project>.Infrastructure \
    --startup-project src/<Project>.Api \
    --output ./scripts/migration.sql
```

### Rules

- **Descriptive names**: `Add_Customer_Table`, `Add_Quotation_Status_Index`, `Backfill_Customer_DefaultRegion`.
- **One concern per migration.** Don't bundle "add table + add index + backfill" in one file.
- **Add a comment block** at the top of each migration describing intent.
- **Review destructive changes** (`DROP COLUMN`, `ALTER TYPE`) — use the **expand-migrate-contract** pattern for breaking changes.
- **Migrations run in CI** via the idempotent script, never via `dotnet ef database update` in prod.

---

## 11. Identity / JWT (when using ASP.NET Identity)

```csharp
public sealed class JwtTokenService(
    IOptions<JwtSettings> settings,
    IClock clock
) : IJwtTokenService
{
    public string GenerateAccessToken(User user, IEnumerable<string> roles, IEnumerable<string> permissions)
    {
        var s = settings.Value;
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.Value.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.CreateVersion7().ToString())
        };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));
        claims.AddRange(permissions.Select(p => new Claim("perm", p)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(s.SigningKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: s.Issuer,
            audience: s.Audience,
            claims: claims,
            expires: clock.UtcNow.UtcDateTime.AddMinutes(s.AccessTokenMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
```

Detail: `05-security/01-authentication-and-authorization.md`.

---

## 12. Folder Layout

```
Infrastructure/
├── Persistence/
│   ├── AppDbContext.cs
│   ├── UnitOfWork.cs                   ← if separate from DbContext
│   ├── Configurations/
│   │   └── <Aggregate>Configuration.cs
│   ├── Migrations/
│   ├── Interceptors/
│   ├── Repositories/
│   │   └── <Aggregate>Repository.cs
│   └── ReadModels/
│       ├── <Aggregate>ReadRepository.cs
│       └── Sql/
├── Caching/
├── Messaging/
│   └── Outbox/
├── Identity/
├── Time/
├── Storage/
├── External/
│   └── <Vendor>/
└── DependencyInjection.cs
```

---

## 13. Common Mistakes

| Mistake                                                            | Fix                                                                  |
|--------------------------------------------------------------------|----------------------------------------------------------------------|
| Inline `modelBuilder.Entity<T>()` in `OnModelCreating`             | One `IEntityTypeConfiguration<T>` per entity                         |
| EF auto-generated constraint names                                 | Explicit `pk_*`, `fk_*`, `uix_*`, `ix_*`                             |
| Repository returns `IQueryable<T>`                                 | Materialize before returning                                         |
| `Include` everything by default                                    | Include only what's required for the aggregate's invariants          |
| Caching write-side entities                                        | Cache projected DTOs only                                            |
| Connection string hard-coded                                       | `IConfiguration` + `IOptions<T>`                                     |
| Migrations bundling many changes                                   | One concern per migration                                            |
| `DateTime.UtcNow` in interceptors                                  | Inject `IClock`                                                      |
