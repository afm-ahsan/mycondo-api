# Error Handling and Logging

This document defines exception types, error responses, logging structure, and observability defaults.

---

## 1. Exception Hierarchy

```
Exception
  ├── DomainException                        ← from Domain layer
  │     ├── InvalidCustomerStateException
  │     ├── DateRangeOverlapException
  │     └── ... (one per business rule)
  │
  ├── ApplicationException (custom)          ← from Application handlers
  │     ├── NotFoundException
  │     ├── ConflictException
  │     └── ForbiddenException
  │
  ├── FluentValidation.ValidationException   ← thrown by ValidationBehavior
  │
  └── (everything else)                      ← unhandled → 500
```

### Where each exception is thrown

| Exception                    | Thrown by              | Maps to HTTP                     |
|------------------------------|------------------------|----------------------------------|
| `DomainException`            | Domain entities        | `422 Unprocessable Entity`       |
| `NotFoundException`          | Application handlers   | `404 Not Found`                  |
| `ConflictException`          | Application handlers   | `409 Conflict`                   |
| `ForbiddenException`         | Application handlers   | `403 Forbidden`                  |
| `ValidationException` (FV)   | `ValidationBehavior`   | `400 Bad Request`                |
| `UnauthorizedAccessException`| ASP.NET / handlers     | `401 Unauthorized`               |
| Anything else                | Anywhere               | `500 Internal Server Error`      |

---

## 2. Defining Exceptions

### Domain exceptions

```csharp
public abstract class DomainException(string message) : Exception(message);

public sealed class InvalidCustomerStateException(CustomerId id, string detail)
    : DomainException($"Customer {id} is in an invalid state: {detail}")
{
    public CustomerId CustomerId { get; } = id;
    public string Detail { get; } = detail;
}
```

### Application exceptions

```csharp
public sealed class NotFoundException : Exception
{
    public string Resource { get; }
    public object Identifier { get; }

    public NotFoundException(string resource, object identifier)
        : base($"{resource} '{identifier}' was not found.")
    {
        Resource = resource;
        Identifier = identifier;
    }
}

public sealed class ConflictException(string message) : Exception(message);
public sealed class ForbiddenException(string message = "Forbidden") : Exception(message);
```

### Rules

- **One exception per business rule.** Don't reuse `InvalidOperationException` for everything.
- **Carry context as properties** (IDs, values) — they're logged automatically.
- **Inherit from `Exception`**, not `ApplicationException` (the BCL one) — it's deprecated.
- **Sealed** — no further subclassing.

---

## 3. Global Exception Middleware

Already covered in `01-backend/05-api-layer.md` §4. Key contract:

```csharp
context.Response.StatusCode = status;
await problemDetailsService.WriteAsync(new ProblemDetailsContext { ... });
```

The response shape is RFC 9457 ProblemDetails JSON:

```json
{
  "type": "https://httpstatuses.io/409",
  "title": "Conflict",
  "status": 409,
  "detail": "Customer with email 'alice@example.com' already exists.",
  "instance": "/api/customers",
  "traceId": "0HMSAGCNNAR4M:00000001"
}
```

### Special cases

#### Validation errors (400)

Validation responses include the field-level breakdown:

```json
{
  "type": "https://httpstatuses.io/400",
  "title": "Validation failed",
  "status": 400,
  "errors": {
    "email": ["Email must be a valid address."],
    "name": ["Name is required."]
  },
  "traceId": "..."
}
```

Implementation: catch `FluentValidation.ValidationException`, format `ve.Errors` into a dictionary keyed by `PropertyName`.

#### Concurrency conflicts (409)

`DbUpdateConcurrencyException` from EF Core maps to `409` with the message "The resource was modified by another user. Please reload and try again."

#### Rate limit (429)

The rate limiter writes ProblemDetails too. Customize via `OnRejected`:

```csharp
options.OnRejected = async (ctx, ct) =>
{
    ctx.HttpContext.Response.StatusCode = 429;
    ctx.HttpContext.Response.Headers["Retry-After"] = "10";
    await ctx.HttpContext.Response.WriteAsJsonAsync(new ProblemDetails
    {
        Status = 429,
        Title = "Too many requests",
        Detail = "You have exceeded the rate limit. Try again later.",
        Type = "https://httpstatuses.io/429"
    }, ct);
};
```

---

## 4. Frontend Error Contract

The frontend's `baseQuery` parses ProblemDetails into a typed `ApiError`:

```ts
export type ApiError =
  | { kind: 'network' }
  | { kind: 'validation'; fields: Record<string, string[]> }
  | { kind: 'unauthorized' }                  // 401
  | { kind: 'forbidden'; message: string }    // 403
  | { kind: 'notFound'; message: string }     // 404
  | { kind: 'conflict'; message: string }     // 409
  | { kind: 'rateLimited'; retryAfter?: number } // 429
  | { kind: 'server'; traceId?: string };     // 5xx
```

Backend and frontend agree on this shape so the frontend can render the right UX without parsing strings.

---

## 5. Logging — Serilog

### Configuration (`appsettings.json`)

```jsonc
{
  "Serilog": {
    "Using": ["Serilog.Sinks.Console", "Serilog.Sinks.File"],
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft.AspNetCore": "Warning",
        "Microsoft.EntityFrameworkCore.Database.Command": "Warning",
        "System.Net.Http.HttpClient": "Warning"
      }
    },
    "Enrich": [
      "FromLogContext",
      "WithMachineName",
      "WithEnvironmentName",
      "WithThreadId"
    ],
    "WriteTo": [
      { "Name": "Console", "Args": { "formatter": "Serilog.Formatting.Compact.RenderedCompactJsonFormatter" } },
      { "Name": "File", "Args": {
          "path": "logs/log-.txt",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 14,
          "formatter": "Serilog.Formatting.Compact.RenderedCompactJsonFormatter"
      }}
    ]
  }
}
```

### Bootstrap

```csharp
builder.Host.UseSerilog((context, services, config) =>
    config.ReadFrom.Configuration(context.Configuration)
          .ReadFrom.Services(services)
          .Enrich.FromLogContext());

app.UseSerilogRequestLogging(opt =>
{
    opt.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
    opt.GetLevel = (httpContext, elapsed, ex) => ex is not null
        ? LogEventLevel.Error
        : httpContext.Response.StatusCode >= 500
            ? LogEventLevel.Error
            : elapsed > 5000
                ? LogEventLevel.Warning
                : LogEventLevel.Information;
    opt.EnrichDiagnosticContext = (diag, http) =>
    {
        diag.Set("UserId", http.User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        diag.Set("CorrelationId", http.Response.Headers["X-Correlation-Id"].ToString());
    };
});
```

### Rules

- **Structured templates only**: `logger.LogInformation("Created {CustomerId}", id)`. Never `logger.LogInformation($"Created {id}")`.
- **Property names are `PascalCase`**, matching the variable.
- **Do not log secrets, tokens, passwords, full credit-card numbers, or PII.**
- **Use `LogContext.PushProperty(...)`** to enrich a scope, not to log the scope itself.

---

## 6. Log Levels — When to Use Which

| Level         | Use case                                                                 |
|---------------|--------------------------------------------------------------------------|
| `Trace`       | Very fine-grained inside a hot loop. Off in prod by default.             |
| `Debug`       | Developer detail (input/output of a calculation). Off in prod default.   |
| `Information` | Business events: "User logged in", "Order created", "Payment processed". |
| `Warning`     | Unusual but handled: slow query, deprecated path, retry succeeded.       |
| `Error`       | Handled exceptions, failed external calls, returned 500 from a handler.  |
| `Critical`    | System-wide failures: DB unreachable, OOM, app crash. Use sparingly.     |

### Rules

- **`Information` for happy-path business events.** Default verbosity in production.
- **`Warning` for things ops should look at but don't page on.**
- **`Error` for exceptions handled by global middleware** that returned a 4xx for a real failure (e.g. 500), or 5xx-mapped DomainExceptions worth investigating.
- **No `Error` log for expected 4xx outcomes** like a missing record. That's noise.

---

## 7. Correlation ID

Every request gets a correlation ID:
- Read from `X-Correlation-Id` header if present.
- Otherwise generated as a UUIDv7.
- Set as response header.
- Pushed into `LogContext` so all downstream logs include it.

The frontend forwards the ID in subsequent requests (e.g. retries) so the entire user-action timeline can be reconstructed in the log backend.

For HTTP calls between services, propagate the ID:

```csharp
public sealed class CorrelationIdHandler(IHttpContextAccessor accessor) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken ct)
    {
        var id = accessor.HttpContext?.Response.Headers["X-Correlation-Id"].ToString();
        if (!string.IsNullOrEmpty(id))
            request.Headers.TryAddWithoutValidation("X-Correlation-Id", id);
        return base.SendAsync(request, ct);
    }
}
```

Register as a `DelegatingHandler` on every typed `HttpClient`.

---

## 8. PII Redaction

Logs are searched and stored for weeks. They MUST NOT contain:
- Full email addresses (mask: `a***@example.com`).
- Phone numbers (mask: `+1 555 *** **89`).
- Government identifiers, credit-card numbers, passwords.
- Health data (HIPAA), financial data (PCI), other regulated fields.

### Implementation

A Serilog `IDestructuringPolicy` redacts properties named `Email`, `Phone`, `Password`, `Ssn`, etc., or carrying a `[Redact]` attribute on the source DTO.

Sensitive fields belong in **structured properties**, never in the message template:

```csharp
// Good: Email is structured, can be redacted.
logger.LogInformation("Login attempt for {Email}", email);

// Bad: email is concatenated into the message; redaction can't find it.
logger.LogInformation($"Login attempt for {email}");
```

---

## 9. OpenTelemetry (Traces, Metrics, Logs)

```csharp
services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService("<Project>.Api"))
    .WithTracing(t => t
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddNpgsql()
        .AddRedisInstrumentation()
        .AddOtlpExporter())
    .WithMetrics(m => m
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()
        .AddOtlpExporter());

builder.Logging.AddOpenTelemetry(o =>
{
    o.IncludeFormattedMessage = true;
    o.IncludeScopes = true;
    o.AddOtlpExporter();
});
```

### Rules

- **OTLP** as the export protocol. The collector decides where to forward (Tempo, Jaeger, Datadog, ...).
- **Service name** matches the `.csproj` name.
- **Trace context propagated** via W3C Trace Context (default).
- **Custom spans** via `Activity` for important business operations:
  ```csharp
  using var activity = ActivitySource.StartActivity("Payment.Process");
  activity?.SetTag("payment.amount", amount);
  ```

---

## 10. Health Checks

Health checks are not logs — they're a separate observability surface for load balancers and orchestrators. Detail: `01-backend/05-api-layer.md` §7.

---

## 11. Alerting Heuristics (for the ops team)

Page on:
- **5xx rate > 1%** of requests over 5 minutes.
- **Error log volume > 10x baseline** in 5 minutes.
- **`/health/ready` failing** for > 1 minute.
- **DB connection pool exhausted** (visible as Npgsql warnings).
- **Outbox processor lag > 1 minute**.

Don't page on:
- Validation errors.
- 404s.
- Rate-limited 429s.

---

## 12. Common Mistakes

| Mistake                                                              | Fix                                                              |
|----------------------------------------------------------------------|------------------------------------------------------------------|
| `logger.LogInformation($"Created {id}")`                             | `logger.LogInformation("Created {Id}", id)`                       |
| `catch (Exception ex) { logger.LogError(ex.Message); }`              | `catch (...) { logger.LogError(ex, "..."); throw; }`              |
| Logging `password`, `token`, `email` raw                             | Redact via destructuring policy                                   |
| `LogError` on 404 / validation failures                              | Log `Information` or `Debug`                                      |
| Missing correlation ID on outbound HTTP                              | `CorrelationIdHandler` as `DelegatingHandler`                     |
| Throwing `Exception("...")` from handlers                            | Throw a specific exception type                                   |
| Generic `try/catch` in handlers                                      | Let middleware handle it                                          |
| Swallowing `OperationCanceledException` from cancellation tokens     | Let it propagate; it indicates client disconnect                  |
