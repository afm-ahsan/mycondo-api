# Backend Testing Strategy

Tests are written **with** the code, not after. A handler without a unit test is a code-review blocker. An endpoint without an integration test is a code-review blocker.

> Stack: xUnit v3 · FluentAssertions · NSubstitute · Testcontainers · Respawn · WebApplicationFactory.

---

## 1. The Test Pyramid

```
                ┌────────────┐
                │     E2E    │   ← few, high-value paths (Playwright on FE)
                └────────────┘
            ┌────────────────────┐
            │    Integration     │   ← per endpoint, real DB via Testcontainers
            └────────────────────┘
        ┌────────────────────────────┐
        │          Unit              │   ← per handler, per aggregate, mocked deps
        └────────────────────────────┘
```

### Coverage targets

| Layer            | Target line coverage |
|------------------|----------------------|
| Domain           | ≥ 90%                |
| Application      | ≥ 90%                |
| Infrastructure   | ≥ 70% (covered mostly via integration)        |
| Api              | ≥ 70% (covered mostly via integration)        |

Coverage gates enforced in CI; PRs that drop coverage > 1% fail.

---

## 2. Unit Tests — Domain

```csharp
public sealed class CustomerTests
{
    private static readonly DateTimeOffset NowUtc = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_WithValidInputs_RaisesCustomerCreatedEvent()
    {
        var email = Email.Create("alice@example.com");

        var customer = Customer.Create("Alice", email, NowUtc);

        customer.Id.Value.Should().NotBeEmpty();
        customer.Status.Should().Be(CustomerStatus.Active);
        customer.DomainEvents
                .Should().ContainSingle()
                .Which.Should().BeOfType<CustomerCreatedEvent>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithBlankName_Throws(string name)
    {
        Action act = () => Customer.Create(name, Email.Create("a@b.com"), NowUtc);

        act.Should().Throw<ArgumentException>()
           .WithMessage("*name*");
    }

    [Fact]
    public void Deactivate_WhenAlreadyInactive_Throws()
    {
        var customer = CustomerBuilder.AnInactiveCustomer().Build();

        Action act = () => customer.Deactivate("test", NowUtc);

        act.Should().Throw<InvalidCustomerStateException>();
    }

    [Fact]
    public void Rename_WithSameName_DoesNotRaiseEvent()
    {
        var customer = CustomerBuilder.AnActiveCustomer("Alice").Build();
        customer.ClearDomainEvents();

        customer.Rename("Alice");

        customer.DomainEvents.Should().BeEmpty();
    }
}
```

### Rules

- **No mocks.** Domain has no dependencies to mock.
- **Use test data builders** (see §6).
- **Test happy path + at least one failure path** per method.
- **Test naming**: `MethodName_Scenario_ExpectedResult`.
- **Arrange-Act-Assert** with blank-line separation.
- **One logical scenario per test.** Multiple `.Should()` for the same concept is fine.

---

## 3. Unit Tests — Application Handlers

```csharp
public sealed class CreateCustomerCommandHandlerTests
{
    private readonly ICustomerRepository _customers = Substitute.For<ICustomerRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IClock _clock = Substitute.For<IClock>();
    private readonly ILogger<CreateCustomerCommandHandler> _logger =
        Substitute.For<ILogger<CreateCustomerCommandHandler>>();

    private readonly CreateCustomerCommandHandler _handler;
    private static readonly DateTimeOffset NowUtc = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public CreateCustomerCommandHandlerTests()
    {
        _clock.UtcNow.Returns(NowUtc);
        _handler = new CreateCustomerCommandHandler(_customers, _uow, _clock, _logger);
    }

    [Fact]
    public async Task Handle_WithUniqueEmail_PersistsAndReturnsDto()
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
    public async Task Handle_WithDuplicateEmail_ThrowsConflictException()
    {
        var command = new CreateCustomerCommand("Alice", "alice@example.com", null);
        _customers.GetByEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>())
                  .Returns(CustomerBuilder.AnActiveCustomer().Build());

        Func<Task> act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
        _customers.DidNotReceive().Add(Arg.Any<Customer>());
        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
```

### Rules

- **Mock only what crosses the layer boundary.** Don't mock value objects or pure functions.
- **`Substitute.For<T>()`** from NSubstitute. Lambda-based, less ceremony than Moq.
- **Verify side effects** with `Received()` / `DidNotReceive()`.
- **Time and IDs are injected** (`IClock`, `IIdGenerator`) so tests are deterministic.
- **One handler under test per file.** If the handler has 5+ scenarios, that's 5+ tests in the same file.

---

## 4. Validators

Validators are easy to forget. Cover them explicitly.

```csharp
public sealed class CreateCustomerCommandValidatorTests
{
    private readonly CreateCustomerCommandValidator _v = new();

    [Fact]
    public void Validate_WithValid_Succeeds()
    {
        var cmd = new CreateCustomerCommand("Alice", "a@b.com", null);
        _v.Validate(cmd).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("", "Name is required.")]
    [InlineData("a", null)]   // length checks pass; just an example
    public void Validate_WithBlankName_Fails(string name, string _)
    {
        var cmd = new CreateCustomerCommand(name, "a@b.com", null);
        var result = _v.Validate(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }
}
```

---

## 5. Integration Tests — Infrastructure

These use **real PostgreSQL** via Testcontainers, not in-memory fakes.

```csharp
public sealed class PostgresContainerFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("test")
        .WithUsername("test")
        .WithPassword("test")
        .Build();

    public string ConnectionString { get; private set; } = default!;

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();
        ConnectionString = _container.GetConnectionString();
        // Apply migrations once at startup.
        var ctx = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(ConnectionString).UseSnakeCaseNamingConvention().Options,
            Substitute.For<IDispatchDomainEvents>());
        await ctx.Database.MigrateAsync();
    }

    public ValueTask DisposeAsync() => _container.DisposeAsync();
}

[Collection("postgres")]
public sealed class CustomerRepositoryTests(PostgresContainerFixture fixture) : IAsyncLifetime
{
    private readonly Respawner _respawner = default!;

    private AppDbContext CreateContext() => new(
        new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(fixture.ConnectionString).UseSnakeCaseNamingConvention().Options,
        Substitute.For<IDispatchDomainEvents>());

    public async ValueTask InitializeAsync()
    {
        await using var conn = new NpgsqlConnection(fixture.ConnectionString);
        await conn.OpenAsync();
        var respawner = await Respawner.CreateAsync(conn, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            SchemasToInclude = new[] { "app" }
        });
        await respawner.ResetAsync(conn);
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task GetByEmailAsync_WhenExists_ReturnsCustomer()
    {
        await using var ctx = CreateContext();
        var customer = Customer.Create("Alice", Email.Create("a@b.com"), DateTimeOffset.UtcNow);
        ctx.Customers.Add(customer);
        await ctx.SaveChangesAsync();

        var repo = new CustomerRepository(ctx);
        var loaded = await repo.GetByEmailAsync(Email.Create("a@b.com"), CancellationToken.None);

        loaded.Should().NotBeNull();
        loaded!.Name.Should().Be("Alice");
    }
}

[CollectionDefinition("postgres")]
public sealed class PostgresCollection : ICollectionFixture<PostgresContainerFixture> { }
```

### Rules

- **Real PostgreSQL via Testcontainers.** Never `UseInMemoryDatabase` — it doesn't enforce constraints, doesn't support snake_case, and lies about transactions.
- **One container per test class collection** (shared via `ICollectionFixture`) — starting Postgres is slow.
- **Reset between tests** with **Respawn**.
- **Migrate once** in `InitializeAsync`.

---

## 6. Test Data Builders

Living in `tests/<Project>.Application.UnitTests/Common/Builders/`:

```csharp
public sealed class CustomerBuilder
{
    private string _name = "Default Customer";
    private Email _email = Email.Create("default@example.com");
    private CustomerStatus _status = CustomerStatus.Active;
    private DateTimeOffset _createdAt = DateTimeOffset.UtcNow;

    public static CustomerBuilder AnActiveCustomer(string name = "Active") =>
        new() { _name = name, _status = CustomerStatus.Active };

    public static CustomerBuilder AnInactiveCustomer(string name = "Inactive") =>
        new() { _name = name, _status = CustomerStatus.Inactive };

    public CustomerBuilder WithEmail(string email)
    {
        _email = Email.Create(email);
        return this;
    }

    public Customer Build()
    {
        var c = Customer.Create(_name, _email, _createdAt);
        if (_status == CustomerStatus.Inactive)
            c.Deactivate("test", _createdAt);
        c.ClearDomainEvents();
        return c;
    }
}
```

### Rules

- **Static factory methods** for the common shapes (`AnActiveCustomer`, `AnInactiveCustomer`).
- **Fluent `With*` setters** for variation.
- **`Build()` is idempotent.** Multiple calls produce equivalent (but distinct) entities.
- **Clear domain events** in `Build()` so tests don't see setup events.

---

## 7. Integration Tests — API

Use `WebApplicationFactory<Program>`:

```csharp
public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine").Build();
    private readonly RedisContainer _redis = new RedisBuilder().Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, cfg) =>
        {
            cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = _postgres.GetConnectionString(),
                ["ConnectionStrings:Redis"] = _redis.GetConnectionString(),
                ["Jwt:SigningKey"] = "test-signing-key-32-chars-minimum-length",
                ["Jwt:Issuer"] = "test", ["Jwt:Audience"] = "test"
            });
        });

        builder.ConfigureTestServices(services =>
        {
            // Replace external clients with fakes here.
        });
    }

    public async ValueTask InitializeAsync()
    {
        await _postgres.StartAsync();
        await _redis.StartAsync();

        // Apply migrations.
        using var scope = Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await ctx.Database.MigrateAsync();
    }

    public new async ValueTask DisposeAsync()
    {
        await _postgres.DisposeAsync();
        await _redis.DisposeAsync();
        await base.DisposeAsync();
    }
}

public sealed class CustomerEndpointsTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private readonly HttpClient _http = factory.CreateClient();

    [Fact]
    public async Task POST_Customers_ReturnsCreatedAndDto()
    {
        var token = await TestAuth.IssueTokenAsync(_http, "alice@example.com");
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var resp = await _http.PostAsJsonAsync("/api/customers",
            new { name = "Alice", email = "new@example.com" });

        resp.StatusCode.Should().Be(HttpStatusCode.Created);
        var dto = await resp.Content.ReadFromJsonAsync<CustomerDto>();
        dto!.Email.Should().Be("new@example.com");
    }

    [Fact]
    public async Task POST_Customers_WithoutAuth_Returns401()
    {
        var resp = await _http.PostAsJsonAsync("/api/customers",
            new { name = "Alice", email = "x@y.com" });

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task POST_Customers_WithInvalidEmail_ReturnsValidationProblem()
    {
        var token = await TestAuth.IssueTokenAsync(_http, "alice@example.com");
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var resp = await _http.PostAsJsonAsync("/api/customers",
            new { name = "Alice", email = "not-an-email" });

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await resp.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        problem!.Errors.Should().ContainKey("Email");
    }
}
```

### Rules

- **Test the full pipeline**: middleware, authentication, validation, handler, persistence.
- **Authenticate per test** using a helper that issues a JWT against the test config.
- **Reset DB between tests** with Respawn (collection fixture).
- **External services mocked at the seam** (typed `HttpClient` replaced via `ConfigureTestServices`).

---

## 8. Architecture Tests (NetArchTest)

Encode the dependency rules and check them in CI:

```csharp
public sealed class ArchitectureTests
{
    [Fact]
    public void Domain_HasNoForbiddenReferences()
    {
        var result = Types.InAssembly(typeof(Customer).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny("Microsoft.EntityFrameworkCore", "MediatR", "Microsoft.AspNetCore")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(string.Join(", ",
            result.FailingTypeNames ?? Array.Empty<string>()));
    }

    [Fact]
    public void Handlers_AreSealed()
    {
        var result = Types.InAssembly(typeof(CreateCustomerCommandHandler).Assembly)
            .That().ImplementInterface(typeof(IRequestHandler<,>))
            .Should().BeSealed()
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }
}
```

### Rules

- **One architecture-test project** at `tests/<Project>.ArchitectureTests/`.
- **Run on every PR.** They prevent layering accidents that compile fine but break the architecture.

---

## 9. What Not to Test

- **EF Core itself.** Trust the framework.
- **Trivial getters / setters.**
- **Configuration class shapes.** Bind validation does this at startup.
- **DI registrations beyond a smoke test.** Integration tests cover this implicitly.

---

## 10. Continuous Integration

```yaml
# .github/workflows/backend.yml (excerpt)
- run: dotnet restore
- run: dotnet build --no-restore -c Release
- run: dotnet test --no-build -c Release \
       --collect:"XPlat Code Coverage" \
       --logger:"trx;LogFilePrefix=test-results"
- uses: codecov/codecov-action@v4
```

### Rules

- **Tests run on every PR.** A red CI blocks merge.
- **Test results uploaded** so failures are visible without re-running locally.
- **Coverage uploaded** to Codecov / Coveralls; PR comment shows delta.

---

## 11. Common Mistakes

| Mistake                                                   | Fix                                                              |
|-----------------------------------------------------------|------------------------------------------------------------------|
| Tests using `UseInMemoryDatabase`                         | Real PostgreSQL via Testcontainers                               |
| Mocking value objects or pure functions                   | Use real instances                                               |
| Multiple "Arrange / Act / Assert" cycles in one test       | Split into separate tests                                       |
| Asserting on log output                                   | Assert on observable behavior; logs are incidental               |
| Tests that depend on each other's state                   | Reset DB between tests; use Respawn                              |
| Tests that hit external services                          | Mock the typed client at the boundary                            |
| Using `DateTime.UtcNow` in tests                          | Inject a fixed `IClock`                                          |
| Tests that use real `Guid.NewGuid()` for assertions       | Inject `IIdGenerator` returning a known value                    |
| Snapshot tests for JSON responses                         | Explicit assertions on the fields you actually care about        |
