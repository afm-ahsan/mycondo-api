# API Layer Conventions

The API layer is the HTTP boundary. It accepts requests, sends commands/queries through MediatR, and translates results into HTTP responses. **It contains no business logic.**

---

## 1. Program.cs

`Program.cs` stays under ~80 lines by extracting setup into extension methods.

```csharp
using <Project>.Api;
using <Project>.Api.Authentication;
using <Project>.Api.HealthChecks;
using <Project>.Api.Middleware;
using <Project>.Api.OpenApi;
using <Project>.Application;
using <Project>.Infrastructure;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, config) =>
    config.ReadFrom.Configuration(context.Configuration)
          .ReadFrom.Services(services)
          .Enrich.FromLogContext());

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration)
    .AddApiServices(builder.Configuration);   // CORS, OpenAPI, Auth, RateLimiting

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseHttpsRedirection();
app.UseCors("DefaultCors");
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<IdempotencyMiddleware>();
app.UseRateLimiter();

app.MapHealthChecks();
app.MapAuthEndpoints();
app.MapCustomerEndpoints();
app.MapQuotationEndpoints();
// ... one Map<Aggregate>Endpoints() per aggregate

app.UseSwaggerInDevelopment();

app.Run();

public partial class Program { }   // For WebApplicationFactory in integration tests
```

### Rules

- **Top-level statements only.** No `Main` method.
- **Order matters.** Correlation ID before exception handler before auth before authorization before idempotency before rate limit.
- **`public partial class Program { }`** at the bottom so integration tests can target it via `WebApplicationFactory<Program>`.

---

## 2. Endpoint Groups

One endpoint group per aggregate, registered as a static extension on `IEndpointRouteBuilder`:

```csharp
public static class CustomerEndpoints
{
    public static IEndpointRouteBuilder MapCustomerEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/customers")
            .WithTags("Customers")
            .RequireAuthorization();

        group.MapPost("/", CreateAsync)
             .WithName("CreateCustomer")
             .WithSummary("Create a new customer")
             .Produces<CustomerDto>(StatusCodes.Status201Created)
             .ProducesValidationProblem()
             .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapGet("/{id:guid}", GetByIdAsync)
             .WithName("GetCustomerById")
             .Produces<CustomerDetailDto>(StatusCodes.Status200OK)
             .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/", SearchAsync)
             .WithName("SearchCustomers")
             .Produces<PagedResult<CustomerSummaryDto>>(StatusCodes.Status200OK);

        group.MapPut("/{id:guid}", UpdateAsync)
             .WithName("UpdateCustomer")
             .Produces<CustomerDto>(StatusCodes.Status200OK)
             .ProducesValidationProblem()
             .ProducesProblem(StatusCodes.Status404NotFound)
             .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapDelete("/{id:guid}", DeleteAsync)
             .WithName("DeleteCustomer")
             .Produces(StatusCodes.Status204NoContent)
             .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> CreateAsync(
        CreateCustomerRequest request,
        IMediator mediator,
        CancellationToken ct)
    {
        var command = new CreateCustomerCommand(request.Name, request.Email, request.Notes);
        var dto = await mediator.Send(command, ct);
        return Results.Created($"/api/customers/{dto.Id}", dto);
    }

    private static async Task<IResult> GetByIdAsync(
        Guid id,
        IMediator mediator,
        CancellationToken ct)
    {
        var dto = await mediator.Send(new GetCustomerByIdQuery(id), ct);
        return Results.Ok(dto);
    }

    private static async Task<IResult> SearchAsync(
        [AsParameters] SearchCustomersRequest request,
        IMediator mediator,
        CancellationToken ct)
    {
        var query = new SearchCustomersQuery(request.Search, request.Page, request.PageSize);
        var page = await mediator.Send(query, ct);
        return Results.Ok(page);
    }

    private static async Task<IResult> UpdateAsync(
        Guid id,
        UpdateCustomerRequest request,
        IMediator mediator,
        CancellationToken ct)
    {
        var command = new UpdateCustomerCommand(id, request.Name, request.Notes);
        var dto = await mediator.Send(command, ct);
        return Results.Ok(dto);
    }

    private static async Task<IResult> DeleteAsync(
        Guid id,
        IMediator mediator,
        CancellationToken ct)
    {
        await mediator.Send(new DeleteCustomerCommand(id), ct);
        return Results.NoContent();
    }
}
```

### Rules

- **Endpoint methods are `private static`** — invocation handlers, not reusable code.
- **Endpoints are thin**: parse → build command → `mediator.Send` → return.
- **No `try/catch`** in endpoints. The middleware handles all exceptions.
- **`Produces<T>(...)` and `ProducesProblem(...)`** on every endpoint for accurate OpenAPI.
- **Use `[AsParameters]`** for query strings with multiple parameters.
- **Route patterns**: `/api/<plural-aggregate>` for collections, `/api/<plural-aggregate>/{id:guid}` for items, `/api/<plural-aggregate>/{id:guid}/<sub-resource>` for nested.

---

## 3. Request and Response DTOs

Request DTOs live in `Api/Contracts/Requests/`:

```csharp
public sealed record CreateCustomerRequest(
    string Name,
    string Email,
    string? Notes);

public sealed record UpdateCustomerRequest(
    string Name,
    string? Notes);

public sealed record SearchCustomersRequest(
    string? Search,
    int Page = 1,
    int PageSize = 20);
```

Response DTOs live in `Application/<Aggregate>s/Dtos/` — they are returned by handlers, so they belong to Application, not Api.

Shared response shapes (`PagedResult<T>`, `ProblemDetails`) live in `Api/Contracts/Responses/`:

```csharp
public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    long Total)
{
    public int TotalPages => (int)Math.Ceiling(Total / (double)PageSize);
    public bool HasNext => Page < TotalPages;
    public bool HasPrevious => Page > 1;
}
```

### Rules

- **Request DTOs are records**, immutable.
- **No nested DTO-of-DTO trees** in requests. Flatten or split into multiple endpoints.
- **JSON serialization** uses camelCase (`PropertyNamingPolicy.CamelCase`) — configured in `Program.cs`.

---

## 4. Middleware

### `GlobalExceptionMiddleware`

Translates exceptions to RFC 9457 ProblemDetails:

```csharp
public sealed class GlobalExceptionMiddleware(
    RequestDelegate next,
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionMiddleware> logger,
    IWebHostEnvironment env)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await HandleAsync(context, ex);
        }
    }

    private async Task HandleAsync(HttpContext context, Exception ex)
    {
        var (status, title, detail) = ex switch
        {
            FluentValidation.ValidationException ve =>
                (StatusCodes.Status400BadRequest, "Validation failed", FormatValidation(ve)),
            NotFoundException nf =>
                (StatusCodes.Status404NotFound, "Resource not found", nf.Message),
            ConflictException cf =>
                (StatusCodes.Status409Conflict, "Conflict", cf.Message),
            ForbiddenException fb =>
                (StatusCodes.Status403Forbidden, "Forbidden", fb.Message),
            DomainException de =>
                (StatusCodes.Status422UnprocessableEntity, "Domain rule violated", de.Message),
            _ =>
                (StatusCodes.Status500InternalServerError, "Internal server error",
                    env.IsDevelopment() ? ex.ToString() : "An unexpected error occurred.")
        };

        if (status >= 500)
            logger.LogError(ex, "Unhandled exception {ExceptionType}", ex.GetType().Name);
        else
            logger.LogWarning("Handled exception {ExceptionType}: {Message}", ex.GetType().Name, ex.Message);

        context.Response.StatusCode = status;
        await problemDetailsService.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails =
            {
                Title = title,
                Detail = detail,
                Status = status,
                Type = $"https://httpstatuses.io/{status}"
            },
            Exception = ex
        });
    }

    private static string FormatValidation(FluentValidation.ValidationException ve) =>
        string.Join("; ", ve.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}"));
}
```

### `CorrelationIdMiddleware`

```csharp
public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    private const string Header = "X-Correlation-Id";

    public async Task InvokeAsync(HttpContext context)
    {
        var id = context.Request.Headers[Header].FirstOrDefault()
                 ?? Guid.CreateVersion7().ToString();

        context.Response.Headers[Header] = id;
        using (Serilog.Context.LogContext.PushProperty("CorrelationId", id))
        {
            await next(context);
        }
    }
}
```

### `IdempotencyMiddleware`

For `POST` and `DELETE` requests carrying an `Idempotency-Key` header, the middleware:
1. Looks up the key in Redis.
2. If present, returns the cached response.
3. Otherwise lets the request through and caches the response.

Detail: `04-api-design/04-error-responses.md`.

### Rules

- **Middleware order**: correlation → exception → HTTPS → CORS → auth → authorization → idempotency → rate limit.
- **No business logic in middleware.** It's plumbing.
- **One responsibility per middleware.**

---

## 5. Authentication and Authorization

```csharp
public static class JwtBearerSetup
{
    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services, IConfiguration cfg)
    {
        var settings = cfg.GetSection("Jwt").Get<JwtSettings>()!;

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = settings.Issuer,
                        ValidAudience = settings.Audience,
                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(settings.SigningKey)),
                        ClockSkew = TimeSpan.FromMinutes(1)
                    };
                });

        services.AddAuthorization(options =>
        {
            options.AddPolicy("CanManageCustomers", p =>
                p.RequireAuthenticatedUser().RequireClaim("perm", "customers.manage"));
            options.AddPolicy("CanViewReports", p =>
                p.RequireAuthenticatedUser().RequireClaim("perm", "reports.view"));
            // Or via PermissionRequirement for dynamic policies — see 05-security/.
        });

        return services;
    }
}
```

Detail: `05-security/01-authentication-and-authorization.md`.

### Rules

- **`RequireAuthorization()`** on every endpoint group by default. Mark public endpoints `AllowAnonymous` explicitly.
- **Permission claims**, not role-name checks. Roles map to permissions; endpoints check permissions.
- **Token validation** explicit — never rely on defaults that may change.

---

## 6. OpenAPI / Swagger

The OpenAPI specification is the single source of truth for the HTTP contract. Backend generates it; frontend consumes it via codegen.

Quick rules:
- **Swashbuckle.AspNetCore** with XML doc comments enabled.
- **Swagger UI** at `/swagger`. **On in Development and Staging; off in Production** (or behind auth) — controlled via `Swagger:Enabled` config flag.
- **JWT bearer security definition** wired so devs can call authorized endpoints from the UI.
- **`AuthorizeOperationFilter`** applies security per-endpoint, respects `[AllowAnonymous]`.
- **Stable `operationId`** via `CustomOperationIds` — drives frontend codegen function names.
- **Every endpoint declares `Produces<T>` and `ProducesProblem(...)`.** Missing metadata = lying OpenAPI = broken codegen.

Full configuration, multi-version setup, operation filters, frontend codegen, and common mistakes: **`04-api-design/04-openapi-swagger.md`**.

---

## 7. Health Checks

```csharp
public static class HealthChecksSetup
{
    public static IServiceCollection AddHealthChecks(
        this IServiceCollection services, IConfiguration cfg)
    {
        services.AddHealthChecks()
            .AddNpgSql(cfg.GetConnectionString("Default")!, name: "postgres", tags: new[] { "ready" })
            .AddRedis(cfg.GetConnectionString("Redis")!, name: "redis", tags: new[] { "ready" });
        return services;
    }

    public static WebApplication MapHealthChecks(this WebApplication app)
    {
        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = _ => false   // Liveness: just "is the process up"
        });
        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = c => c.Tags.Contains("ready"),
            ResponseWriter = WriteHealthResponse
        });
        return app;
    }

    private static Task WriteHealthResponse(HttpContext ctx, HealthReport report) =>
        ctx.Response.WriteAsJsonAsync(new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description
            })
        });
}
```

### Rules

- **`/health/live`** is "is the process alive". Returns 200 unless the process is broken.
- **`/health/ready`** is "is this instance ready to receive traffic". Checks DB, Redis, etc.
- **No authentication** on health endpoints.
- **Production load balancer** uses `/health/ready` for traffic routing.

---

## 8. CORS

```csharp
services.AddCors(options =>
{
    options.AddPolicy("DefaultCors", builder =>
    {
        builder
            .WithOrigins(configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()!)
            .AllowCredentials()
            .AllowAnyHeader()
            .AllowAnyMethod()
            .WithExposedHeaders("X-Correlation-Id");
    });
});
```

### Rules

- **No `AllowAnyOrigin()` in production.** Origins are whitelisted via configuration.
- **`AllowCredentials()`** is required if using cookies; in that case `AllowAnyOrigin` is forbidden by browsers anyway.
- **Expose correlation ID** so the frontend can log it.

---

## 9. Rate Limiting

```csharp
services.AddRateLimiter(opt =>
{
    opt.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    opt.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
        RateLimitPartition.GetTokenBucketLimiter(
            partitionKey: ctx.User.Identity?.Name ?? ctx.Connection.RemoteIpAddress?.ToString() ?? "anon",
            factory: _ => new TokenBucketRateLimiterOptions
            {
                TokenLimit = 100,
                ReplenishmentPeriod = TimeSpan.FromSeconds(10),
                TokensPerPeriod = 50,
                AutoReplenishment = true,
                QueueLimit = 0
            }));
});

app.UseRateLimiter();
```

### Rules

- **Per-user when authenticated, per-IP otherwise.**
- **Stricter limits on auth endpoints** (login, register, refresh) — 5 per minute is typical.
- **429 response** uses ProblemDetails format like other errors.

---

## 10. Configuration

```csharp
services.AddOptions<JwtSettings>()
        .BindConfiguration("Jwt")
        .ValidateDataAnnotations()
        .ValidateOnStart();

public sealed record JwtSettings
{
    [Required] public string Issuer { get; init; } = default!;
    [Required] public string Audience { get; init; } = default!;
    [Required, MinLength(32)] public string SigningKey { get; init; } = default!;
    [Range(1, 60)] public int AccessTokenMinutes { get; init; } = 15;
    [Range(1, 30)] public int RefreshTokenDays { get; init; } = 7;
}
```

### Rules

- **`IOptions<T>` with validation** for every settings group.
- **`ValidateOnStart()`** — bad config fails the app at boot, not at first request.
- **Settings classes are records with init-only properties.**
- **No magic strings for config keys.** Use `BindConfiguration("<Section>")`.

---

## 11. Folder Layout

```
Api/
├── Program.cs
├── appsettings.json
├── appsettings.Development.json
├── Endpoints/
│   ├── AuthEndpoints.cs
│   ├── CustomerEndpoints.cs
│   └── ...
├── Contracts/
│   ├── Requests/
│   └── Responses/
├── Middleware/
│   ├── GlobalExceptionMiddleware.cs
│   ├── CorrelationIdMiddleware.cs
│   └── IdempotencyMiddleware.cs
├── Authentication/
│   ├── JwtBearerSetup.cs
│   ├── AuthorizationPolicies.cs
│   └── PermissionRequirement.cs
├── OpenApi/
│   ├── SwaggerSetup.cs
│   └── AuthorizeOperationFilter.cs
├── HealthChecks/
│   ├── HealthChecksSetup.cs
│   └── DatabaseHealthCheck.cs
└── DependencyInjection.cs   ← AddApiServices(IConfiguration)
```

---

## 12. Common Mistakes

| Mistake                                               | Fix                                                                |
|-------------------------------------------------------|--------------------------------------------------------------------|
| `try/catch` in endpoints                              | Trust the middleware                                               |
| Business logic in endpoints                           | Move to a handler                                                  |
| `Results.Ok(entity)` returning aggregate roots        | Return DTOs                                                        |
| Magic status codes                                    | `StatusCodes.Status201Created`                                     |
| `[FromQuery]` everywhere on a single param            | Use `[AsParameters]` for many; default binding for one             |
| Returning `void` from a delete endpoint               | `Results.NoContent()` (204)                                        |
| Inconsistent error responses                          | Always ProblemDetails via `GlobalExceptionMiddleware`              |
| CORS `AllowAnyOrigin` in production                   | Whitelist via config                                               |
| Missing `Produces` on endpoints                       | OpenAPI lies; clients break                                        |
