# Domain Layer Conventions

The Domain project is the **most protected** code in the solution. It encodes the business invariants, knows nothing about persistence or transport, and is the easiest layer to test.

> Replace `<Aggregate>` with your aggregate root names (`Customer`, `Invoice`, `Quotation`).

---

## 1. Forbidden Dependencies

The `Domain` `.csproj` may reference **only**:
- The .NET base class library.
- `<Project>.Shared` (if it exists, with no upward references).

Forbidden NuGet references:
- `Microsoft.EntityFrameworkCore` (and any EF Core sub-package).
- `MediatR`.
- `Microsoft.AspNetCore.*`.
- `System.Net.Http`.
- `Newtonsoft.Json` / `System.Text.Json` (DTOs and JSON live in Application/Api).
- Any third-party logging library — Domain doesn't log.

CI runs an architecture test that fails the build if any of these appear.

---

## 2. Aggregate Roots

An aggregate root is a cluster of entities and value objects with a single entry point that enforces invariants.

```csharp
public sealed class Customer : AggregateRoot<CustomerId>
{
    public string Name { get; private set; }
    public Email Email { get; private set; }
    public CustomerStatus Status { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public int Version { get; private set; }

    private readonly List<ContactPerson> _contactPersons = new();
    public IReadOnlyList<ContactPerson> ContactPersons => _contactPersons.AsReadOnly();

    // EF Core needs a parameterless constructor; mark private
    private Customer() { Name = null!; Email = null!; }

    private Customer(CustomerId id, string name, Email email, DateTimeOffset nowUtc) : base(id)
    {
        Name = name;
        Email = email;
        Status = CustomerStatus.Active;
        CreatedAtUtc = nowUtc;
        Version = 1;
    }

    public static Customer Create(string name, Email email, DateTimeOffset nowUtc)
    {
        Guard.AgainstNullOrWhiteSpace(name);
        Guard.AgainstNull(email);

        var customer = new Customer(CustomerId.New(), name, email, nowUtc);
        customer.RaiseDomainEvent(new CustomerCreatedEvent(customer.Id, nowUtc));
        return customer;
    }

    public void Rename(string newName)
    {
        Guard.AgainstNullOrWhiteSpace(newName);
        if (Name == newName) return;

        Name = newName;
        Version++;
        RaiseDomainEvent(new CustomerRenamedEvent(Id, newName));
    }

    public void Deactivate(string reason, DateTimeOffset nowUtc)
    {
        if (Status == CustomerStatus.Inactive)
            throw new InvalidCustomerStateException(Id, "Customer already inactive");

        Status = CustomerStatus.Inactive;
        Version++;
        RaiseDomainEvent(new CustomerDeactivatedEvent(Id, reason, nowUtc));
    }
}
```

### Rules

- **`sealed`** on every aggregate. Inheritance is opt-in and justified.
- **All setters are `private set`.** State changes through methods only.
- **Constructors are private** except for the parameterless one EF Core needs.
- **A `static Create(...)`** factory is the only way to construct an aggregate. It validates inputs and raises a `Created` event.
- **Methods enforce invariants**, not callers. `customer.Deactivate(reason)` checks the state, not the handler that calls it.
- **Increment `Version`** on every state-changing method.
- **Raise domain events** to signal what happened, not to coordinate.

---

## 3. Strongly-Typed IDs

Avoid `Guid` parameter pollution. Use a `readonly record struct` per aggregate:

```csharp
public readonly record struct CustomerId(Guid Value)
{
    public static CustomerId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();

    public static CustomerId Parse(string s) =>
        Guid.TryParse(s, out var g)
            ? new CustomerId(g)
            : throw new FormatException($"Invalid CustomerId: '{s}'");
}
```

### Rules

- **UUIDv7** (`Guid.CreateVersion7()`) — sortable, index-friendly.
- **`readonly record struct`** for zero allocation and value equality.
- **Configure EF Core** to map the type via `HasConversion(id => id.Value, value => new CustomerId(value))`.
- **JSON converters** are registered in `<Project>.Api/Program.cs` for automatic serialization.

---

## 4. Value Objects

Replace primitive obsession (`string`, `decimal`, `DateTime`) with semantic types.

```csharp
public sealed record Email
{
    public string Value { get; }

    private Email(string value) => Value = value;

    public static Email Create(string raw)
    {
        Guard.AgainstNullOrWhiteSpace(raw);
        var trimmed = raw.Trim().ToLowerInvariant();
        if (!IsValid(trimmed))
            throw new InvalidEmailFormatException(raw);
        return new Email(trimmed);
    }

    public override string ToString() => Value;

    private static bool IsValid(string s) =>
        s.Contains('@') && s.Length <= 320 && /* ... */ true;
}

public sealed record Money(decimal Amount, string Currency)
{
    public static Money Zero(string currency) => new(0m, currency);

    public Money Add(Money other)
    {
        if (Currency != other.Currency)
            throw new CurrencyMismatchException(Currency, other.Currency);
        return new Money(Amount + other.Amount, Currency);
    }
}

public sealed record DateRange(DateTimeOffset Start, DateTimeOffset End)
{
    public bool Overlaps(DateRange other) => Start < other.End && other.Start < End;
    public TimeSpan Duration => End - Start;

    public static DateRange Create(DateTimeOffset start, DateTimeOffset end)
    {
        if (end <= start)
            throw new InvalidDateRangeException(start, end);
        return new DateRange(start, end);
    }
}
```

### Rules

- **Records or `record struct`** — value equality is the whole point.
- **Private constructors + static `Create`** for validation.
- **No setters.** Operations return new instances (`money.Add(other)` returns a new `Money`).
- **EF Core mapping** via `Owned()` for complex value objects, or via `HasConversion(...)` for single-property ones.

---

## 5. Domain Events

Events describe **what happened**, in past tense. They're immutable records.

```csharp
public interface IDomainEvent
{
    Guid EventId => Guid.CreateVersion7();
    DateTimeOffset OccurredAtUtc { get; }
}

public sealed record CustomerCreatedEvent(
    CustomerId CustomerId,
    DateTimeOffset OccurredAtUtc) : IDomainEvent;

public sealed record CustomerDeactivatedEvent(
    CustomerId CustomerId,
    string Reason,
    DateTimeOffset OccurredAtUtc) : IDomainEvent;
```

### Rules

- **Past tense**: `CustomerCreatedEvent`, not `CreateCustomerEvent`.
- **Immutable** — records.
- **Carry IDs and the change**, not full entity snapshots.
- **Live in `<Aggregate>s/Events/`** next to the aggregate that raises them.
- **Dispatched on save** by `DispatchDomainEventsInterceptor` in Infrastructure — never manually.
- **Handled in Application** (`<Aggregate>s/EventHandlers/`).

---

## 6. Domain Services

Use a **domain service** when behavior:
- Spans multiple aggregates and doesn't naturally belong to either, **or**
- Requires external state the aggregate shouldn't know about.

```csharp
public interface IInvoiceNumberGenerator
{
    Task<string> GenerateNextAsync(int year, CancellationToken ct);
}
```

The interface lives in `Domain/Abstractions/`. The implementation lives in `Infrastructure/`.

### Rules

- **Stateless and injectable.** No singletons holding domain state.
- **Use sparingly.** Prefer behavior on the aggregate. A handler with three injected `I*Service` interfaces is a smell — push the logic into the aggregate.

---

## 7. Domain Exceptions

```csharp
public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message) { }
    protected DomainException(string message, Exception inner) : base(message, inner) { }
}

public sealed class InvalidCustomerStateException : DomainException
{
    public CustomerId CustomerId { get; }
    public InvalidCustomerStateException(CustomerId id, string detail)
        : base($"Customer {id} is in an invalid state: {detail}")
    {
        CustomerId = id;
    }
}
```

### Rules

- **Specific exception per business rule.** `DomainException` is abstract.
- **Carry context** (IDs, values) as properties.
- **Translated to HTTP** by `GlobalExceptionMiddleware` (typically `409 Conflict` or `422 Unprocessable Entity`).
- **Never caught inside Domain.** Let them bubble to the Application layer.

---

## 8. Repositories (Abstraction Only)

The interface lives in Domain. The implementation lives in Infrastructure.

```csharp
public interface ICustomerRepository
{
    Task<Customer?> GetByIdAsync(CustomerId id, CancellationToken ct);
    Task<Customer?> GetByEmailAsync(Email email, CancellationToken ct);
    void Add(Customer customer);
    void Remove(Customer customer);
}

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct);
}
```

### Rules

- **One repository per aggregate root**, never per entity.
- **`GetByIdAsync` returns nullable.** Don't throw `NotFoundException` from the repo — let the handler decide.
- **No `IQueryable<T>` exposed.** That leaks EF Core abstractions into Application.
- **`Add`/`Remove` are synchronous** — they mutate the EF change tracker; the actual DB write happens in `IUnitOfWork.SaveChangesAsync`.

---

## 9. Time and IDs

Two abstractions are used everywhere in Domain when needed:

```csharp
public interface IClock
{
    DateTimeOffset UtcNow { get; }
}

public interface IIdGenerator
{
    Guid NewUuidV7();
}
```

### Rules

- **Never use `DateTime.UtcNow`** in domain code. Use `IClock`.
- **Never use `Guid.NewGuid()`** for aggregate IDs. Use `Guid.CreateVersion7()` (or `IIdGenerator` if testability matters).
- **Reasoning:** time and IDs must be deterministic in tests.

---

## 10. Folder Layout (Per Aggregate)

```
Domain/
└── Customers/
    ├── Customer.cs              ← aggregate root
    ├── CustomerId.cs            ← strongly-typed ID
    ├── CustomerStatus.cs        ← state enum (or smart enum)
    ├── ContactPerson.cs         ← child entity
    ├── Email.cs                 ← value object (when private to this aggregate)
    └── Events/
        ├── CustomerCreatedEvent.cs
        ├── CustomerRenamedEvent.cs
        └── CustomerDeactivatedEvent.cs
```

If the value object is shared across aggregates (`Money`, `Address`), put it in `Domain/Common/ValueObjects/` instead.

---

## 11. Domain Tests

Domain is the easiest layer to test — no mocks needed.

```csharp
public sealed class CustomerTests
{
    [Fact]
    public void Create_WithValidInputs_RaisesCustomerCreatedEvent()
    {
        var nowUtc = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var email = Email.Create("alice@example.com");

        var customer = Customer.Create("Alice", email, nowUtc);

        customer.DomainEvents
            .Should().ContainSingle()
            .Which.Should().BeOfType<CustomerCreatedEvent>();
    }

    [Fact]
    public void Deactivate_WhenAlreadyInactive_Throws()
    {
        var customer = CustomerBuilder.AnInactiveCustomer().Build();

        Action act = () => customer.Deactivate("test", DateTimeOffset.UtcNow);

        act.Should().Throw<InvalidCustomerStateException>();
    }
}
```

### Rules

- **No mocks.** Domain has no dependencies to mock.
- **Test data builders** in `tests/<Project>.Application.UnitTests/Common/Builders/`.
- **Coverage target: 90%+** on Domain. It's pure logic — there's no excuse not to test it.
- **Test method names** use `MethodName_Scenario_ExpectedResult`.

---

## 12. Common Mistakes (and Their Fixes)

| Mistake                                                            | Fix                                                                  |
|--------------------------------------------------------------------|----------------------------------------------------------------------|
| Public setters on entities                                         | Private setters; mutate via methods                                  |
| Aggregate references the DbContext                                 | Repository abstraction in Domain; impl in Infrastructure             |
| `static` helpers in Domain holding state                           | Inject the dependency; remove static state                           |
| `Guid` parameters everywhere                                       | Strongly-typed IDs                                                   |
| `decimal price` floating around                                    | `Money` value object                                                 |
| Logic in handlers that should be on the aggregate                  | Move it into a method on the aggregate                               |
| Domain event names like `CreateCustomer`                           | `CustomerCreated` (past tense)                                       |
| Throwing `Exception` or `InvalidOperationException`                | Specific `DomainException` subclass                                  |
| `DateTime.UtcNow` in domain code                                   | Inject `IClock`                                                      |
| `Guid.NewGuid()` for aggregate IDs                                 | `Guid.CreateVersion7()`                                              |
