# Backend Coding Standards (.NET / C#)

This document is the day-to-day code-quality rulebook. Every rule is enforced by Roslyn analyzers, `.editorconfig`, or CI where possible.

---

## 1. Naming

| Kind                            | Convention                  | Example                              |
|---------------------------------|-----------------------------|--------------------------------------|
| Namespace                       | `PascalCase`, matches folder| `<Project>.Application.Customers.Commands.CreateCustomer` |
| Type (class/struct/record)      | `PascalCase`                | `CreateCustomerCommand`              |
| Interface                       | `IPascalCase`               | `ICustomerRepository`                |
| Method                          | `PascalCase`, verb-first    | `CreateAsync`, `CalculateTotal`      |
| Property                        | `PascalCase`                | `CreatedAtUtc`, `IsActive`           |
| Field — private                 | `_camelCase`                | `_customers`, `_unitOfWork`          |
| Field — `const` / `static readonly` | `PascalCase`            | `MaxRetryCount`                      |
| Local variable / parameter      | `camelCase`                 | `customerId`, `command`              |
| Async method                    | suffix `Async`              | `GetByIdAsync`                       |
| Generic param                   | `T` or `TKey`/`TValue`      | `Result<TValue, TError>`             |
| File                            | matches the public type     | `CreateCustomerCommand.cs`           |

### Forbidden

- **Hungarian notation**: `strName`, `bIsActive`, `intCount`.
- **Abbreviations** except universally known: `Id`, `Url`, `Http`, `Json`, `Xml`, `Db`, `Api`.
- **`Manager`, `Helper`, `Utility`, `Stuff`, `Data`, `Info`** as type-name suffixes — they signal a missing abstraction.

---

## 2. Type Design

### Records vs Classes vs Structs

- **Records (or record structs)** — DTOs, commands, queries, value objects, events. Anything immutable + by-value equality.
- **Classes** — aggregate roots, services, repositories, anything with identity or behavior.
- **`record struct` / `readonly record struct`** — strongly-typed IDs and small value objects to avoid heap allocation.

### Sealing

- **`sealed` by default** on classes that aren't designed for inheritance.
- **Inheritance is opt-in**, justified, documented.
- **Roslyn analyzer flags** non-sealed concrete classes (CA1852 enabled).

### Immutability

- **Domain entities expose only `private set` properties.** State changes through methods.
- **Collections exposed as `IReadOnlyList<T>` / `IReadOnlyCollection<T>`**, never `List<T>`.
- **DTOs and value objects** are records — immutable by construction.

### Nullability

- **Every reference type is non-nullable** unless explicitly marked `?`.
- **Repositories return nullable** for "find by id" methods (`Task<Customer?> GetByIdAsync(...)`).
- **Public APIs validate inputs** via guard clauses (`ArgumentNullException.ThrowIfNull`, `ArgumentException.ThrowIfNullOrWhiteSpace`).

---

## 3. Method Design

- **Method body length**: aim for ≤ 30 lines. Long methods are a refactoring signal.
- **Parameter count**: ≤ 4 parameters. Beyond that, introduce a parameter object (record).
- **Return early.** No deeply nested `if/else` pyramids; prefer guard clauses.
- **Async all the way.** No `.Result`, `.Wait()`, `.GetAwaiter().GetResult()`. CI fails on these.
- **`CancellationToken`** is the **last parameter** of every async method, named `ct` or `cancellationToken`.
- **Static where possible.** Pure helpers go on `static` types.

### Async Rules

- **All public async methods have the `Async` suffix** and accept `CancellationToken`.
- **`ConfigureAwait(false)` is not needed** in ASP.NET Core (no `SynchronizationContext` since .NET Core 3.x). Don't add it.
- **`ValueTask` only when profiling shows it matters.** Default to `Task`.
- **`IAsyncEnumerable<T>`** for streaming reads; `Task<List<T>>` for bounded result sets.

---

## 4. Concurrency and Threading

- **Never `lock` over async code.** Use `SemaphoreSlim` if in-process synchronization is needed.
- **Distributed locks via Redis** (`IDistributedLockProvider`).
- **Optimistic concurrency** via `Version` column on aggregates is the default.
- **No `Task.Run` in ASP.NET Core request handlers.** It steals threads from the request pool.
- **No `async void`** except event handlers. CI fails on `async void` in business code.

---

## 5. Error Handling

### Exception layers

| Layer            | Exception base                | Maps to                                |
|------------------|-------------------------------|----------------------------------------|
| Domain           | `DomainException`             | `422 Unprocessable Entity`             |
| Application      | `NotFoundException`           | `404 Not Found`                        |
|                  | `ConflictException`           | `409 Conflict`                         |
|                  | `ForbiddenException`          | `403 Forbidden`                        |
|                  | `ValidationException` (FV)    | `400 Bad Request`                      |
| System           | unhandled                     | `500 Internal Server Error`            |

### Rules

- **Never catch `Exception` and swallow it.** Catch specific or rethrow with context.
- **No exception filters that hide stack traces.**
- **Errors flow through `GlobalExceptionMiddleware`** → RFC 9457 ProblemDetails. Detail: `01-backend/05-api-layer.md` §4.
- **Specific exception per business rule.** A handler throws `ConflictException("Email already in use")`, not `Exception("...")`.

---

## 6. Logging and Observability

- **Use `Microsoft.Extensions.Logging`** with **structured templates**:
  ```csharp
  logger.LogInformation("Created customer {CustomerId} for {OwnerId}", id, ownerId);
  ```
- **No `string.Format`, no string interpolation in log messages.** Structured properties only — they enable searching by field in the log backend.
- **Log levels**:
  - `LogTrace`: very fine-grained, off in prod.
  - `LogDebug`: developer detail, off in prod by default.
  - `LogInformation`: business events ("created", "logged in").
  - `LogWarning`: unusual but handled (slow query, deprecated path).
  - `LogError`: handled exceptions, failed external calls.
  - `LogCritical`: system-wide failures, used sparingly.
- **Correlation ID** propagated via `X-Correlation-Id` header and `Serilog.Context.LogContext` scope.
- **No PII in logs.** Redact emails, phone numbers, IDs subject to regulation. Use a redaction enricher.
- **OpenTelemetry** is configured in `Program.cs`: traces, metrics, logs flow through OTLP.

---

## 7. EF Core Discipline

- **One `DbContext` per bounded context.** No god-context.
- **Entity configurations in `IEntityTypeConfiguration<T>`** files — never inline in `OnModelCreating`.
- **No lazy loading.** Eager-load via `Include`/`ThenInclude` or use projection.
- **No tracking on read paths.** `AsNoTracking()` for queries; or use Dapper.
- **Use compiled queries** (`EF.CompileAsyncQuery`) for high-throughput repeated queries.
- **Transactions are explicit** via `IUnitOfWork.SaveChangesAsync` — handlers don't manage transactions directly.
- **Migrations reviewed** for `DROP COLUMN`, `ALTER COLUMN TYPE` — use **expand → migrate → contract** for breaking schema changes.
- **Detail**: `03-database/02-ef-core-configurations.md`.

---

## 8. Configuration and Secrets

- **All config goes through `IOptions<T>`** with validation.
- **No magic strings for config keys.** Use `nameof` or constants.
- **Secrets never committed.** Local dev uses `dotnet user-secrets`; staging/prod use Azure Key Vault / AWS Secrets Manager / GitHub Secrets.
- **Connection strings are environment-specific.** Production uses managed identity where possible.

---

## 9. Cache and Lock Key Naming

Cache and distributed-lock keys follow a strict naming pattern so they don't collide across services:

```
<service>:<aggregate>:<id>:<purpose>
```

Examples:
- `srm:customer:abc123:detail` — cached customer detail DTO.
- `srm:invoice:def456:lock` — distributed lock for invoice processing.
- `srm:outbox:processor:lock` — singleton lock for outbox processor.

Constants in `Application/Common/Constants/CacheKeys.cs`:

```csharp
public static class CacheKeys
{
    public const string Service = "srm";

    public static string CustomerDetail(Guid id) => $"{Service}:customer:{id}:detail";
    public static string InvoiceLock(Guid id) => $"{Service}:invoice:{id}:lock";
}
```

---

## 10. Performance

- **Use `Span<T>`/`Memory<T>`** for hot paths that parse/format data. Don't pre-optimize cold paths.
- **`StringBuilder`** for >3 string concatenations in a loop.
- **Pool large objects** (`ArrayPool<T>`) when allocation profiling shows pressure.
- **Profile before optimizing.** Use BenchmarkDotNet for micro-benchmarks; PerfView / dotnet-trace for macro-profiling.
- **EF Core**: `AsNoTracking`, projections, compiled queries on hot paths.
- **Cache aggressively** with Redis on the read path; invalidate on the write path.

---

## 11. Security in Code

- **All external input validated** at the API boundary (FluentValidation).
- **Parameterize all SQL.** Dapper and EF parameterize by default; raw SQL via `FromSqlInterpolated` is the only acceptable form.
- **No `eval`, no dynamic code generation** from user input.
- **Authentication / Authorization** via `[Authorize]` or `RequireAuthorization()` on endpoint groups. Default to deny.
- **HTTPS only.** HSTS enabled.
- **Rate limiting** at the API gateway and per-endpoint via `Microsoft.AspNetCore.RateLimiting`.
- **Idempotency** for `POST` and `DELETE` requests via `Idempotency-Key` header.
- **Detail**: `05-security/`.

---

## 12. Comments and Documentation

- **XML doc comments** on all public types and members in `Application/Abstractions/` and `Domain/Abstractions/`.
- **Comments explain *why*, not *what*.** The code says what; the comment says why it's not the obvious thing.
- **TODO comments include a ticket reference**: `// TODO(<TICKET-ID>): <description>`.
- **No commented-out code** in commits. Use Git history.

---

## 13. File Organization Inside a Class

Order members consistently:

```csharp
public sealed class Foo
{
    // 1. Constants and static readonly fields
    private const int MaxRetries = 3;

    // 2. Private fields (readonly first)
    private readonly IBar _bar;
    private int _counter;

    // 3. Constructors
    public Foo(IBar bar) => _bar = bar;

    // 4. Public properties
    public bool IsActive { get; private set; }

    // 5. Public methods (entry points first)
    public Task DoAsync(CancellationToken ct) { ... }

    // 6. Private methods (in order of call from public methods)
    private void Helper() { ... }

    // 7. Nested types (rare; use sparingly)
}
```

---

## 14. C# 14 Features to Prefer

- **Primary constructors** over manual ctor wiring:
  ```csharp
  public sealed class Service(IRepo repo, IClock clock)
  {
      public Task DoAsync() => repo.LoadAsync(clock.UtcNow);
  }
  ```
- **Collection expressions**:
  ```csharp
  IReadOnlyList<int> items = [1, 2, 3];
  ```
- **Required members** for DTOs that need certain properties set:
  ```csharp
  public sealed record Settings
  {
      public required string ApiKey { get; init; }
      public required string Endpoint { get; init; }
  }
  ```
- **`field` keyword** in property accessors when you need a backing field but want to avoid declaring one.
- **Pattern matching** in switch expressions for exception mapping (see `01-backend/05-api-layer.md` §4).

---

## 15. Forbidden Patterns

| Pattern                                                         | Use instead                              |
|------------------------------------------------------------------|------------------------------------------|
| `Task.Result`, `.Wait()`, `.GetAwaiter().GetResult()`            | `await`                                  |
| `async void` (non-event-handler)                                 | `async Task`                             |
| `catch (Exception) { }` (swallow)                                | Catch specific or rethrow with context   |
| Public mutable fields                                            | Properties with private setters          |
| `Singleton` services holding mutable state                       | Scoped/transient or thread-safe state    |
| Service Locator / `IServiceProvider.GetService` in business code | Constructor injection                    |
| Static state in business logic                                   | Injected dependencies                    |
| `dynamic`                                                        | Strong types or generics                 |
| `string.Format` in logs                                          | Structured logging templates             |
| Raw `Guid.NewGuid()` for aggregate IDs                           | `Guid.CreateVersion7()`                  |
| `DateTime.UtcNow` in domain code                                 | `IClock`                                 |
| Hard-coded connection strings / URLs                             | `IConfiguration` + `IOptions<T>`         |
| Repositories returning `IQueryable<T>`                           | Materialized results (`List<T>`, DTO)    |
| `AutoMapper` for DTO mapping                                     | Manual `ToDto()` extension methods       |
| Generic `Manager`/`Helper`/`Utility` classes                     | Specific abstraction with intent         |
| Abbreviated names (`cust`, `usr`, `ord`)                         | Full names (`customer`, `user`, `order`) |

---

## 16. Pull Request Checklist

Before merging:

- [ ] Build clean: zero warnings, zero analyzer violations.
- [ ] All tests pass; new logic has unit tests; new endpoints have integration tests.
- [ ] No `Task.Result`, `.Wait()`, `async void`, or `catch (Exception)` swallows.
- [ ] No new EF Core lazy-loading or N+1 patterns.
- [ ] Migrations reviewed for destructive changes; rollback path documented.
- [ ] No PII or secrets in logs.
- [ ] Public abstractions have XML doc comments.
- [ ] OpenAPI generation succeeds; new endpoints documented.
- [ ] Cache keys, lock keys, idempotency keys follow `<service>:<scope>:<id>:<purpose>` pattern.
