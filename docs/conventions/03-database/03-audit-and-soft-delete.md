# Audit and Soft Delete

Every business table tracks who changed what, when, and supports logical deletion.

---

## 1. Audit Columns (Mandatory on Every Business Table)

```csharp
public interface IAuditable
{
    DateTimeOffset CreatedAtUtc { get; set; }
    DateTimeOffset UpdatedAtUtc { get; set; }
    Guid? CreatedBy { get; set; }
    Guid? UpdatedBy { get; set; }
}

public abstract class AggregateRoot<TId> : Entity<TId>, IAuditable, ISoftDeletable
{
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }
    public DateTimeOffset? DeletedAtUtc { get; set; }
    public int Version { get; set; } = 1;
    // ... domain events list
}
```

### Rules

- **Every aggregate root has audit columns.** No exceptions for "internal" entities.
- **Set automatically** by `AuditInterceptor` — handlers don't touch them.
- **`CreatedBy` / `UpdatedBy` are nullable** because system jobs (seeder, outbox processor) write rows without a user.

---

## 2. AuditInterceptor

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
        if (eventData.Context is null)
            return base.SavingChangesAsync(eventData, result, ct);

        var nowUtc = clock.UtcNow;
        var userId = currentUser.UserId?.Value;

        foreach (var entry in eventData.Context.ChangeTracker.Entries<IAuditable>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAtUtc = nowUtc;
                    entry.Entity.UpdatedAtUtc = nowUtc;
                    entry.Entity.CreatedBy = userId;
                    entry.Entity.UpdatedBy = userId;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAtUtc = nowUtc;
                    entry.Entity.UpdatedBy = userId;
                    // Prevent overwriting CreatedAtUtc / CreatedBy
                    entry.Property(nameof(IAuditable.CreatedAtUtc)).IsModified = false;
                    entry.Property(nameof(IAuditable.CreatedBy)).IsModified = false;
                    break;
            }
        }

        return base.SavingChangesAsync(eventData, result, ct);
    }
}
```

### Rules

- **Registered in `AddInfrastructure`** as Scoped.
- **Reads `IClock`** so tests can use a fixed time.
- **Reads `ICurrentUserProvider`** — populated by ASP.NET Identity middleware.
- **Protects `CreatedAtUtc` / `CreatedBy`** from being overwritten on update.

---

## 3. Soft Delete

Soft delete = set `deleted_at_utc` instead of removing the row. Hard delete is reserved for retention jobs.

```csharp
public interface ISoftDeletable
{
    DateTimeOffset? DeletedAtUtc { get; set; }
}
```

### Aggregate-level support

Aggregates expose a `Delete()` method that calls the EF tracker's `Remove()`:

```csharp
public void Delete()
{
    if (DeletedAtUtc is not null) return;
    RaiseDomainEvent(new <Aggregate>DeletedEvent(Id, DateTimeOffset.UtcNow));
    // The repository / interceptor converts Remove() into a soft delete:
}
```

In the handler:

```csharp
public async Task Handle(DeleteCustomerCommand command, CancellationToken ct)
{
    var customer = await customers.GetByIdAsync(new CustomerId(command.Id), ct)
        ?? throw new NotFoundException(nameof(Customer), command.Id);

    customer.Delete();
    customers.Remove(customer);
    await uow.SaveChangesAsync(ct);
}
```

The `SoftDeleteInterceptor` translates `Remove()` into setting `DeletedAtUtc`:

```csharp
public sealed class SoftDeleteInterceptor(IClock clock) : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken ct = default)
    {
        if (eventData.Context is null)
            return base.SavingChangesAsync(eventData, result, ct);

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

### Query filter

EF Core query filter automatically excludes soft-deleted rows:

```csharp
builder.HasQueryFilter(x => x.DeletedAtUtc == null);
```

To intentionally include soft-deleted rows in a query (e.g. an admin "restore deleted" page):

```csharp
db.Customers.IgnoreQueryFilters()
```

### Rules

- **Every business aggregate is soft-deletable** unless it has a regulatory reason to be hard-deleted.
- **Query filter is automatic.** Tests must apply migrations to a real DB so the filter behaves correctly.
- **Restore endpoint** if the business needs it: clears `DeletedAtUtc` and raises `<Aggregate>RestoredEvent`.
- **Hard delete** lives in a retention job, not in the API.

---

## 4. Optimistic Concurrency

The `Version` column is a concurrency token. Every UPDATE increments it; if two requests touch the same row, the second one fails with `DbUpdateConcurrencyException`.

```csharp
public sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.Property(x => x.Version).IsConcurrencyToken();
    }
}
```

The aggregate's mutating methods increment `Version`:

```csharp
public void Rename(string newName)
{
    Name = newName;
    Version++;
    RaiseDomainEvent(new CustomerRenamedEvent(Id, newName));
}
```

The middleware translates `DbUpdateConcurrencyException` to `409 Conflict`:

```json
{
  "type": "https://httpstatuses.io/409",
  "title": "Conflict",
  "status": 409,
  "detail": "The resource was modified by another user. Please reload and try again."
}
```

### Rules

- **`Version` is mandatory** on every aggregate.
- **Increment in the domain method**, never in the handler.
- **Frontend retry** on 409 happens explicitly — show a "reload and try again" UX, don't auto-retry.

---

## 5. Audit Log Table (Regulated Domains)

For tables containing PHI / PII / financial data, add a row-level history.

### Schema

```sql
CREATE TABLE audit.audit_log (
    id                uuid PRIMARY KEY,
    occurred_at_utc   timestamptz NOT NULL DEFAULT now(),
    actor_id          uuid,                     -- who did it
    correlation_id    uuid,                     -- request correlation id
    schema_name       text NOT NULL,
    table_name        text NOT NULL,
    record_id         uuid NOT NULL,
    operation         text NOT NULL CHECK (operation IN ('INSERT','UPDATE','DELETE')),
    before_json       jsonb,
    after_json        jsonb
);

CREATE INDEX ix_audit_log_table_record
  ON audit.audit_log (schema_name, table_name, record_id, occurred_at_utc DESC);

CREATE INDEX ix_audit_log_actor
  ON audit.audit_log (actor_id, occurred_at_utc DESC);
```

### Approach 1: App-level (recommended for most cases)

The `AuditInterceptor` writes to `audit.audit_log` in the same transaction as the entity change. Pros: portable, easy to test. Cons: app must be the only writer.

### Approach 2: PostgreSQL triggers

A trigger function writes to `audit.audit_log` on every INSERT/UPDATE/DELETE.

```sql
CREATE OR REPLACE FUNCTION audit.fn_tg_audit() RETURNS trigger AS $$
DECLARE
    v_actor uuid := NULLIF(current_setting('app.actor_id', true), '')::uuid;
    v_correlation uuid := NULLIF(current_setting('app.correlation_id', true), '')::uuid;
BEGIN
    INSERT INTO audit.audit_log(
        id, actor_id, correlation_id, schema_name, table_name, record_id,
        operation, before_json, after_json)
    VALUES (
        gen_random_uuid(), v_actor, v_correlation,
        TG_TABLE_SCHEMA, TG_TABLE_NAME,
        COALESCE((NEW).id, (OLD).id),
        TG_OP,
        CASE WHEN TG_OP IN ('UPDATE','DELETE') THEN row_to_json(OLD)::jsonb ELSE NULL END,
        CASE WHEN TG_OP IN ('INSERT','UPDATE') THEN row_to_json(NEW)::jsonb ELSE NULL END);
    RETURN COALESCE(NEW, OLD);
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER tg_customer_audit
AFTER INSERT OR UPDATE OR DELETE ON app.customer
FOR EACH ROW EXECUTE FUNCTION audit.fn_tg_audit();
```

Pros: catches all writes (incl. direct DB access). Cons: harder to test, trigger code is in SQL.

### Rules

- **Pick one approach per project.** Don't mix.
- **App-level audit is the default.** Triggers only when there's a regulatory requirement that mandates DB-level enforcement.
- **`actor_id` and `correlation_id`** are the breadcrumbs — without them, the log is much less useful.

---

## 6. Auditing Reads (PHI / PII)

For data subject to HIPAA / GDPR access logs, log reads too:

```csharp
public sealed class AccessLoggingBehavior<TRequest, TResponse>(
    IAccessLogger logger,
    ICurrentUserProvider currentUser
) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>, ISensitiveQuery
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        var response = await next();
        await logger.LogReadAsync(currentUser.UserId, request, ct);
        return response;
    }
}
```

`ISensitiveQuery` is a marker interface on queries that touch regulated data. The handler runs the query; the behavior logs that the user accessed it.

### Rules

- **Access logs go to the same `audit.audit_log` table** with `operation = 'READ'`.
- **Don't log every list query** — only individual record retrieval (where the user has actually viewed the data).
- **Retention** of access logs follows the regulatory minimum (e.g. HIPAA = 6 years).

---

## 7. Retention and Hard Delete

For data with a retention policy, a **scheduled job** (BackgroundService or external cron) hard-deletes after the policy window:

```csharp
public sealed class RetentionWorker(
    IServiceScopeFactory scopes,
    IClock clock,
    ILogger<RetentionWorker> logger
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        var period = TimeSpan.FromHours(24);
        var timer = new PeriodicTimer(period);
        while (await timer.WaitForNextTickAsync(ct))
        {
            using var scope = scopes.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var threshold = clock.UtcNow.AddYears(-7);     // 7-year retention

            var deleted = await db.Customers
                .IgnoreQueryFilters()
                .Where(c => c.DeletedAtUtc != null && c.DeletedAtUtc < threshold)
                .ExecuteDeleteAsync(ct);

            logger.LogInformation("Retention: hard-deleted {Count} customers", deleted);
        }
    }
}
```

### Rules

- **Retention runs out-of-band**, not from API requests.
- **Document the retention period per table** in the migration that introduces the table.
- **Audit log retention is separate** — usually longer (regulatory minimum).

---

## 8. Restore Endpoint (Optional)

If users can restore soft-deleted records:

```csharp
public sealed record RestoreCustomerCommand(Guid Id) : IRequest<CustomerDto>;

public sealed class RestoreCustomerCommandHandler(...) : IRequestHandler<RestoreCustomerCommand, CustomerDto>
{
    public async Task<CustomerDto> Handle(RestoreCustomerCommand command, CancellationToken ct)
    {
        var customer = await db.Customers
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Id == new CustomerId(command.Id), ct)
            ?? throw new NotFoundException(nameof(Customer), command.Id);

        if (customer.DeletedAtUtc is null)
            throw new ConflictException("Customer is not deleted.");

        customer.Restore();   // sets DeletedAtUtc = null, raises CustomerRestoredEvent
        await uow.SaveChangesAsync(ct);

        return customer.ToDto();
    }
}
```

### Rules

- **Permission-gated.** Only users with `customers.restore` permission can call it.
- **Audit logged** — restoring is itself an audit-worthy operation.
- **Domain event** raised so other modules (e.g. integrations) know.

---

## 9. Common Mistakes

| Mistake                                                       | Fix                                                              |
|---------------------------------------------------------------|------------------------------------------------------------------|
| Setting `CreatedAtUtc` / `UpdatedAtUtc` in handler            | Trust the `AuditInterceptor`                                     |
| Using `DELETE FROM ...` in app code                           | Aggregate's `Delete()` + `Remove()` (interceptor soft-deletes)   |
| Forgetting query filter — soft-deleted rows appear            | `HasQueryFilter(x => x.DeletedAtUtc == null)`                    |
| Increment `Version` in handler instead of aggregate method    | Keep state changes inside the aggregate                          |
| Auditing every `SELECT` — log spam                            | Mark sensitive queries with `ISensitiveQuery`                    |
| Hard-deleting from API for "performance"                      | Soft delete; retention job hard-deletes later                    |
| Audit log writeable from app code                             | Append-only; CHECK constraint or trigger enforces                |
| Triggering audit + app-audit (double-write)                   | Pick one approach                                                |
| Restoring without an audit entry                              | Restore is auditable                                             |
