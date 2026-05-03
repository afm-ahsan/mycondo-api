# Application Layer Conventions

The Application layer is the **orchestrator**. It receives commands and queries, validates them, calls the Domain to do the work, and persists changes through the abstractions Infrastructure provides.

> Replace `<Aggregate>` with the aggregate root name (e.g. `Customer`).

---

## 1. Allowed Dependencies

The `Application` `.csproj` may reference:
- `<Project>.Domain`
- `<Project>.Shared` (if exists)
- `MediatR`
- `FluentValidation`
- `Microsoft.Extensions.Logging.Abstractions`
- `Microsoft.Extensions.Caching.Abstractions` (for `ICacheService` definition)

Forbidden:
- `Microsoft.EntityFrameworkCore` — Application doesn't know about EF.
- `Microsoft.AspNetCore.*` — Application doesn't know about HTTP.
- `Npgsql.*`, `StackExchange.Redis` — those are Infrastructure concerns.

---

## 2. Commands

A command **changes state**. It is named in imperative form: `CreateCustomer`, `DeactivateCustomer`, `RenameCustomer`.

```csharp
public sealed record CreateCustomerCommand(
    string Name,
    string Email,
    string? Notes
) : IRequest<CustomerDto>;
```

### Rules

- **`sealed record`** — immutable, value equality.
- **Imperative name + `Command` suffix.**
- **Returns the DTO** the caller needs back (typically `<Aggregate>Dto` for create/update).
- **No nullables for required fields** — required is required. Use `string?` only for genuinely optional inputs.
- **Primitives at the boundary** — accept `string Email`, validate it inside the handler/validator before constructing the `Email` value object.

### File layout (one folder per use case)

```
Application/
└── Customers/
    └── Commands/
        └── CreateCustomer/
            ├── CreateCustomerCommand.cs
            ├── CreateCustomerCommandHandler.cs
            └── CreateCustomerCommandValidator.cs
```

---

## 3. Command Validators

Validators run **before** the handler via the `ValidationBehavior` pipeline. A missing validator fails CI.

```csharp
public sealed class CreateCustomerCommandValidator : AbstractValidator<CreateCustomerCommand>
{
    public CreateCustomerCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(320);

        RuleFor(x => x.Notes)
            .MaximumLength(1000);
    }
}
```

### Rules

- **Pure structural validation.** Required, length, format. No DB calls.
- **Async DB checks** (e.g. "is this email already taken?") happen in the **handler**, not the validator. Validators stay fast and pure.
- **One validator per command.** Compose with `Include(...)` if you need shared rules.
- **Localize messages** with FluentValidation's `LanguageManager` when applicable.

---

## 4. Command Handlers

```csharp
public sealed class CreateCustomerCommandHandler(
    ICustomerRepository customers,
    IUnitOfWork unitOfWork,
    IClock clock,
    ILogger<CreateCustomerCommandHandler> logger
) : IRequestHandler<CreateCustomerCommand, CustomerDto>
{
    public async Task<CustomerDto> Handle(
        CreateCustomerCommand command,
        CancellationToken ct)
    {
        var email = Email.Create(command.Email);

        var existing = await customers.GetByEmailAsync(email, ct);
        if (existing is not null)
            throw new ConflictException($"Customer with email '{email}' already exists.");

        var customer = Customer.Create(command.Name, email, clock.UtcNow);
        customers.Add(customer);

        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation(
            "Customer {CustomerId} created with email {Email}",
            customer.Id, email);

        return customer.ToDto();
    }
}
```

### Rules

- **`sealed`** on every handler.
- **C# 14 primary constructor** for dependency injection — no manual ctor wiring.
- **Single responsibility.** A handler orchestrates the domain. It does not contain business logic; that lives on the aggregate.
- **No HTTP types** (`HttpContext`, `IFormFile`) and **no DB-specific code** (`DbContext`, raw SQL) in handlers.
- **Throw application/domain exceptions** for failure cases. The middleware translates them to HTTP.
- **Log structured.** Use `{Property}` placeholders, never string interpolation.
- **`CancellationToken`** is the last parameter, named `ct`.

---

## 5. Queries

A query **reads state**. It is named in question form: `GetCustomerById`, `SearchCustomers`, `ListActiveQuotations`.

```csharp
public sealed record GetCustomerByIdQuery(Guid CustomerId) : IRequest<CustomerDetailDto>;

public sealed class GetCustomerByIdQueryHandler(
    ICustomerReadRepository readRepo
) : IRequestHandler<GetCustomerByIdQuery, CustomerDetailDto>
{
    public async Task<CustomerDetailDto> Handle(
        GetCustomerByIdQuery query,
        CancellationToken ct)
    {
        var dto = await readRepo.GetByIdAsync(new CustomerId(query.CustomerId), ct);
        return dto ?? throw new NotFoundException(nameof(Customer), query.CustomerId);
    }
}
```

### Rules

- **Queries return DTOs**, never entities.
- **Use a separate read repository** (`ICustomerReadRepository`) backed by Dapper or EF `AsNoTracking`. Don't reuse the write-side repo.
- **`NotFoundException`** is thrown by the handler when a single-resource query finds nothing. List queries return an empty list.
- **No invariant checks** — queries are read-only.

---

## 6. DTOs

Data Transfer Objects are flat, immutable records that cross layer boundaries.

```csharp
public sealed record CustomerDto(
    Guid Id,
    string Name,
    string Email,
    string Status,
    DateTimeOffset CreatedAtUtc);

public sealed record CustomerDetailDto(
    Guid Id,
    string Name,
    string Email,
    string Status,
    IReadOnlyList<ContactPersonDto> ContactPersons,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record ContactPersonDto(
    Guid Id,
    string Name,
    string PhoneNumber,
    bool IsPrimary);
```

### Rules

- **`sealed record`** — immutable.
- **Primitive types** at the boundary (`string`, `Guid`, `DateTimeOffset`). The strongly-typed IDs and value objects belong inside Domain; DTOs are flat.
- **DTOs live in `Application/<Aggregate>s/Dtos/`** (or in `Common/Dtos/` if cross-aggregate).
- **Mapping**: a manual `ToDto()` extension method per aggregate, hand-written. **No AutoMapper.** Manual mapping is debuggable and AOT-friendly.

```csharp
internal static class CustomerMapping
{
    public static CustomerDto ToDto(this Customer c) => new(
        c.Id.Value,
        c.Name,
        c.Email.Value,
        c.Status.ToString(),
        c.CreatedAtUtc);
}
```

---

## 7. MediatR Pipeline Behaviors

Behaviors run for every request, in registration order. Standard set:

```
Request
  │
  ▼
UnhandledExceptionBehavior   ← logs and rethrows unexpected exceptions
  │
  ▼
LoggingBehavior              ← logs request name + correlation ID
  │
  ▼
ValidationBehavior           ← runs FluentValidation; throws ValidationException
  │
  ▼
PerformanceBehavior          ← warns if handler exceeds N ms
  │
  ▼
Handler
```

### `ValidationBehavior` (sketch)

```csharp
public sealed class ValidationBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators
) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        if (!validators.Any()) return await next();

        var context = new ValidationContext<TRequest>(request);
        var failures = (await Task.WhenAll(
                validators.Select(v => v.ValidateAsync(context, ct))))
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count != 0)
            throw new ValidationException(failures);

        return await next();
    }
}
```

### `PerformanceBehavior` (sketch)

```csharp
public sealed class PerformanceBehavior<TRequest, TResponse>(
    ILogger<PerformanceBehavior<TRequest, TResponse>> logger
) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private const int SlowRequestThresholdMs = 500;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        var response = await next();
        sw.Stop();

        if (sw.ElapsedMilliseconds > SlowRequestThresholdMs)
        {
            logger.LogWarning(
                "Slow request {RequestName} took {Elapsed} ms",
                typeof(TRequest).Name, sw.ElapsedMilliseconds);
        }

        return response;
    }
}
```

### Rules

- **All four behaviors registered in order** in `Application/DependencyInjection.cs`.
- **Never** put business logic in a behavior — that's a handler's job.
- **Behaviors are generic and stateless.**

---

## 8. Application-Level Exceptions

```csharp
public abstract class ApplicationException : Exception
{
    protected ApplicationException(string message) : base(message) { }
}

public sealed class NotFoundException : ApplicationException
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

public sealed class ConflictException : ApplicationException
{
    public ConflictException(string message) : base(message) { }
}

public sealed class ForbiddenException : ApplicationException
{
    public ForbiddenException(string message = "Forbidden") : base(message) { }
}
```

### Rules

- **Three classes are usually enough**: `NotFoundException`, `ConflictException`, `ForbiddenException`. Plus `FluentValidation.ValidationException` for validation failures.
- **Domain exceptions** (e.g. `InvalidCustomerStateException`) flow through unchanged — `GlobalExceptionMiddleware` maps them too.
- **Don't catch and rethrow** with a generic message. Let the original exception flow up; the middleware handles it.

---

## 9. Cross-Cutting Abstractions (Application/Common/Abstractions)

These are interfaces the Application defines and Infrastructure implements:

```csharp
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken ct);
    Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct);
    Task RemoveAsync(string key, CancellationToken ct);
}

public interface IDistributedLockProvider
{
    Task<IAsyncDisposable?> TryAcquireAsync(string key, TimeSpan timeout, CancellationToken ct);
}

public interface IMessageBus
{
    Task PublishAsync<TMessage>(TMessage message, CancellationToken ct) where TMessage : class;
}

public interface ICurrentUserProvider
{
    UserId? UserId { get; }
    bool IsAuthenticated { get; }
    bool IsInRole(string role);
    bool HasPermission(string permission);
}
```

### Rules

- **Interface in Application; impl in Infrastructure.** Infrastructure registers the impl in `AddInfrastructure(...)`.
- **Cache keys** follow the pattern `<aggregate>:<id>:<version>` (see `01-backend/06-coding-standards.md`).
- **Lock keys** follow `<aggregate>:<id>:<purpose>`.

---

## 10. Domain Event Handlers

When a domain event needs to trigger side effects (sending an email, publishing to a bus), handle it in `Application/<Aggregate>s/EventHandlers/`:

```csharp
public sealed class CustomerCreatedEventHandler(
    IMessageBus bus,
    ILogger<CustomerCreatedEventHandler> logger
) : INotificationHandler<CustomerCreatedEvent>
{
    public async Task Handle(CustomerCreatedEvent @event, CancellationToken ct)
    {
        await bus.PublishAsync(new CustomerOnboardedIntegrationEvent(@event.CustomerId), ct);

        logger.LogInformation(
            "CustomerCreatedEvent handled for {CustomerId}",
            @event.CustomerId);
    }
}
```

### Rules

- **Domain event = inside this service.** Integration event = published to other services via the bus.
- **Idempotent handlers**: if dispatched twice, the result must be the same. Use a `processed_message` table if needed.
- **No throws inside event handlers** — they break the calling transaction. Catch and log; let the outbox retry.

---

## 11. Dependency Injection

`Application/DependencyInjection.cs` exposes a single extension:

```csharp
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
            cfg.AddOpenBehavior(typeof(UnhandledExceptionBehavior<,>));
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            cfg.AddOpenBehavior(typeof(PerformanceBehavior<,>));
        });

        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);

        return services;
    }
}
```

`Api/Program.cs` calls:

```csharp
builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration);
```

---

## 12. Folder Layout (Per Aggregate)

```
Application/
└── Customers/
    ├── Commands/
    │   ├── CreateCustomer/
    │   │   ├── CreateCustomerCommand.cs
    │   │   ├── CreateCustomerCommandHandler.cs
    │   │   └── CreateCustomerCommandValidator.cs
    │   ├── UpdateCustomer/
    │   ├── DeactivateCustomer/
    │   └── DeleteCustomer/
    ├── Queries/
    │   ├── GetCustomerById/
    │   ├── SearchCustomers/
    │   └── ListContactPersons/
    ├── EventHandlers/
    │   └── CustomerCreatedEventHandler.cs
    └── Dtos/
        ├── CustomerDto.cs
        ├── CustomerDetailDto.cs
        └── ContactPersonDto.cs
```

---

## 13. Application Tests

Application handlers are the most-tested code in the solution after Domain.

```csharp
public sealed class CreateCustomerCommandHandlerTests
{
    private readonly ICustomerRepository _customers = Substitute.For<ICustomerRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IClock _clock = Substitute.For<IClock>();
    private readonly ILogger<CreateCustomerCommandHandler> _logger =
        Substitute.For<ILogger<CreateCustomerCommandHandler>>();

    private readonly CreateCustomerCommandHandler _handler;

    public CreateCustomerCommandHandlerTests()
    {
        _clock.UtcNow.Returns(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        _handler = new CreateCustomerCommandHandler(_customers, _uow, _clock, _logger);
    }

    [Fact]
    public async Task Handle_WithUniqueEmail_CreatesCustomer()
    {
        var command = new CreateCustomerCommand("Alice", "alice@example.com", null);
        _customers.GetByEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>())
                  .Returns((Customer?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Email.Should().Be("alice@example.com");
        _customers.Received(1).Add(Arg.Any<Customer>());
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithDuplicateEmail_ThrowsConflict()
    {
        var command = new CreateCustomerCommand("Alice", "alice@example.com", null);
        _customers.GetByEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>())
                  .Returns(CustomerBuilder.AnyCustomer().Build());

        Func<Task> act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }
}
```

### Rules

- **NSubstitute** for mocks. **FluentAssertions** for assertions.
- **Test data builders** for aggregates: `CustomerBuilder.AnyCustomer().With...().Build()`.
- **One logical scenario per test.** A test should fail for one reason.
- **Coverage target: 90%+** on Application handlers.
