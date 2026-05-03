# Backend Solution Structure

**Stack**: .NET 10 LTS · C# 14 · ASP.NET Core 10 (Minimal APIs) · EF Core 10 · MediatR · FluentValidation · PostgreSQL · Redis
**Architecture**: Clean Architecture + CQRS + DDD-lite

> Use this layout for **every** new .NET service. Folder names are non-negotiable. Replace `<Project>` with your solution name (e.g. `Acme.Billing`, `SRMedical`) and `<Aggregate>` with the domain aggregate (`Order`, `Invoice`, `Customer`).

---

## 1. Solution Root Layout

```
<Project>/
├── .editorconfig                      # Formatting + analyzer rules (mandatory)
├── .gitignore
├── .gitattributes
├── Directory.Build.props              # Centralized: TargetFramework, Nullable, LangVersion
├── Directory.Packages.props           # Centralized NuGet versions (CPM enabled)
├── global.json                        # SDK pin: { "version": "10.0.x", "rollForward": "latestFeature" }
├── <Project>.sln                      # Or <Project>.slnx (preferred in .NET 10)
├── README.md
├── CLAUDE.md                          # AI assistant memory (see 08-templates/)
├── docker-compose.yml                 # Local dev: api + postgres + redis
├── Dockerfile                         # Multi-stage: sdk → runtime
│
├── src/
│   ├── <Project>.Domain/
│   ├── <Project>.Application/
│   ├── <Project>.Infrastructure/
│   ├── <Project>.Api/
│   ├── <Project>.Shared/              # Truly cross-cutting primitives
│   └── <Project>.Workers/             # Background services (optional)
│
├── tests/
│   ├── <Project>.Domain.UnitTests/
│   ├── <Project>.Application.UnitTests/
│   ├── <Project>.Infrastructure.IntegrationTests/
│   └── <Project>.Api.IntegrationTests/
│
└── docs/
    ├── architecture/
    ├── decisions/                     # ADRs
    ├── database/
    └── deployment/
```

---

## 2. Mandatory Root Files

### `Directory.Build.props`

Sets defaults for every project in the solution:

```xml
<Project>
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <AnalysisLevel>latest-recommended</AnalysisLevel>
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <NoWarn>$(NoWarn);CS1591</NoWarn> <!-- Missing XML doc on internals -->
  </PropertyGroup>
</Project>
```

### `Directory.Packages.props`

Centrally Managed Packages (CPM) is **mandatory**. Every NuGet version is declared here, never in individual `.csproj` files:

```xml
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>

  <ItemGroup>
    <PackageVersion Include="MediatR" Version="..." />
    <PackageVersion Include="FluentValidation.AspNetCore" Version="..." />
    <PackageVersion Include="Serilog.AspNetCore" Version="..." />
    <PackageVersion Include="Microsoft.EntityFrameworkCore" Version="..." />
    <PackageVersion Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="..." />
    <PackageVersion Include="EFCore.NamingConventions" Version="..." />
    <PackageVersion Include="StackExchange.Redis" Version="..." />
    <PackageVersion Include="Dapper" Version="..." />
    <PackageVersion Include="Swashbuckle.AspNetCore" Version="..." />
    <PackageVersion Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="..." />
    <PackageVersion Include="BCrypt.Net-Next" Version="..." />
    <PackageVersion Include="OpenTelemetry.Extensions.Hosting" Version="..." />
    <!-- ... -->
  </ItemGroup>
</Project>
```

### `global.json`

```json
{
  "sdk": {
    "version": "10.0.100",
    "rollForward": "latestFeature"
  }
}
```

### `.editorconfig`

The `.editorconfig` enforces formatting and analyzer severity. The full file is templated; key entries:

```ini
root = true

[*.cs]
indent_style = space
indent_size = 4
end_of_line = crlf
insert_final_newline = true
trim_trailing_whitespace = true

# Naming
dotnet_naming_rule.private_fields_should_begin_with_underscore.severity = error
dotnet_naming_rule.private_fields_should_begin_with_underscore.symbols  = private_fields
dotnet_naming_rule.private_fields_should_begin_with_underscore.style    = camel_case_with_underscore

# Severity overrides
dotnet_diagnostic.CA2007.severity = none   # ConfigureAwait not needed in ASP.NET Core
dotnet_diagnostic.CS1591.severity = suggestion
```

---

## 3. Domain Project — `<Project>.Domain`

Pure C#. **Zero** external dependencies (no EF Core, no MediatR, no ASP.NET).

```
<Project>.Domain/
├── Common/
│   ├── Entity.cs                      # Base entity (Id, equality)
│   ├── AggregateRoot.cs               # Base aggregate root (raises domain events)
│   ├── ValueObject.cs                 # Base value object
│   ├── DomainEvent.cs                 # IDomainEvent marker
│   └── Result.cs                      # Optional Result<T> helper
│
├── <Aggregate>s/                      # One folder per aggregate (folder is plural)
│   ├── <Aggregate>.cs                 # Aggregate root (singular)
│   ├── <Aggregate>Id.cs               # Strongly-typed ID (record struct)
│   ├── <ValueObject>.cs               # Aggregate-private value objects
│   └── Events/
│       ├── <Aggregate>CreatedEvent.cs
│       └── <Aggregate>UpdatedEvent.cs
│
├── Abstractions/                      # Interfaces implemented by Infrastructure
│   ├── IUnitOfWork.cs
│   ├── I<Aggregate>Repository.cs
│   ├── IClock.cs
│   └── IIdGenerator.cs
│
└── Exceptions/
    ├── DomainException.cs             # Base
    └── <Specific>DomainException.cs
```

### Rules

- **One aggregate per folder.** Folder name is plural (`Customers/`), the root is singular (`Customer.cs`).
- **Strongly-typed IDs** as `readonly record struct` (`public readonly record struct CustomerId(Guid Value)`).
- **All entity setters are `private`.** State changes through methods (`order.Submit()`, `order.Cancel(reason)`).
- **Domain events** in `Events/` next to the aggregate that raises them.
- **Forbidden references:** `Microsoft.EntityFrameworkCore`, `MediatR`, `Microsoft.AspNetCore.*`, `System.Net.Http`. CI fails on violations.

---

## 4. Application Project — `<Project>.Application`

CQRS use cases. Depends on `Domain`. No EF Core, no DB drivers, no HTTP.

```
<Project>.Application/
├── Common/
│   ├── Behaviors/
│   │   ├── ValidationBehavior.cs              # FluentValidation pipeline
│   │   ├── LoggingBehavior.cs
│   │   ├── PerformanceBehavior.cs             # Warns on > N ms
│   │   └── UnhandledExceptionBehavior.cs
│   ├── Abstractions/
│   │   ├── ICacheService.cs
│   │   ├── IDistributedLockProvider.cs
│   │   ├── IMessageBus.cs
│   │   ├── ICurrentUserProvider.cs
│   │   └── IFileStorage.cs
│   ├── Exceptions/
│   │   ├── ApplicationException.cs
│   │   ├── NotFoundException.cs
│   │   ├── ConflictException.cs
│   │   └── ForbiddenException.cs
│   └── DependencyInjection.cs                  # AddApplication() extension
│
├── <Aggregate>s/                               # One folder per aggregate
│   ├── Commands/
│   │   ├── Create<Aggregate>/
│   │   │   ├── Create<Aggregate>Command.cs
│   │   │   ├── Create<Aggregate>CommandHandler.cs
│   │   │   └── Create<Aggregate>CommandValidator.cs
│   │   └── Update<Aggregate>/
│   ├── Queries/
│   │   ├── Get<Aggregate>ById/
│   │   │   ├── Get<Aggregate>ByIdQuery.cs
│   │   │   ├── Get<Aggregate>ByIdQueryHandler.cs
│   │   │   └── Get<Aggregate>ByIdQueryValidator.cs
│   │   └── Search<Aggregate>s/
│   ├── EventHandlers/
│   │   └── <Aggregate>CreatedEventHandler.cs
│   └── Dtos/
│       └── <Aggregate>Dto.cs
```

### Rules

- **One folder per use case.** Each contains exactly: `Command.cs`, `CommandHandler.cs`, `CommandValidator.cs` (or query equivalents).
- **Commands return DTOs**, never entities.
- **Validators are mandatory** for commands; missing validators fail CI.
- **Use `sealed`** on every handler and command class.
- **C# 14 primary constructors** preferred over manual constructor wiring.

---

## 5. Infrastructure Project — `<Project>.Infrastructure`

```
<Project>.Infrastructure/
├── Persistence/
│   ├── AppDbContext.cs
│   ├── Configurations/                         # IEntityTypeConfiguration<T>
│   │   └── <Aggregate>Configuration.cs
│   ├── Migrations/                             # EF Core migrations
│   ├── Interceptors/
│   │   ├── DispatchDomainEventsInterceptor.cs
│   │   ├── AuditInterceptor.cs
│   │   └── SoftDeleteInterceptor.cs
│   ├── Repositories/
│   │   └── <Aggregate>Repository.cs
│   ├── ReadModels/                             # Dapper-based read queries
│   │   ├── <Aggregate>ReadRepository.cs
│   │   └── Sql/                                # Complex SQL files
│   └── UnitOfWork.cs
│
├── Caching/
│   ├── RedisCacheService.cs
│   └── RedisDistributedLockProvider.cs
│
├── Messaging/                                  # When using a broker
│   ├── RabbitMqMessageBus.cs
│   └── Outbox/
│       ├── OutboxMessage.cs
│       ├── OutboxProcessor.cs                  # BackgroundService
│       └── OutboxConfiguration.cs
│
├── Identity/
│   ├── JwtTokenService.cs
│   ├── PasswordHasher.cs
│   ├── CurrentUserProvider.cs
│   └── IdentitySeeder.cs
│
├── Time/
│   └── SystemClock.cs                          # IClock implementation
│
├── Storage/                                    # File / blob storage
│   └── LocalFileStorage.cs
│
├── External/                                   # 3rd-party clients
│   └── <Vendor>Client.cs
│
└── DependencyInjection.cs                      # AddInfrastructure(IConfiguration)
```

### Rules

- **One configuration class per entity.** Never inline `modelBuilder.Entity<T>()` calls in `OnModelCreating`.
- **Migrations are added with descriptive names**: `dotnet ef migrations add Add_Customer_Table`.
- **All connection strings, secrets, and feature flags** come from `IConfiguration` — never hard-coded.
- **Repositories return materialized results** (`List<T>`, DTO), never `IQueryable<T>`.

---

## 6. API Project — `<Project>.Api`

```
<Project>.Api/
├── Program.cs                                  # Top-level statements
├── appsettings.json
├── appsettings.Development.json
├── appsettings.Production.json
│
├── Endpoints/                                  # Minimal API groups
│   ├── <Aggregate>Endpoints.cs                 # static IEndpointRouteBuilder ext
│   └── EndpointsExtensions.cs                  # Common endpoint helpers
│
├── Contracts/                                  # Request/response types
│   ├── Requests/
│   │   ├── Create<Aggregate>Request.cs
│   │   └── Update<Aggregate>Request.cs
│   └── Responses/
│       ├── PagedResult.cs
│       └── ProblemDetailsResponse.cs
│
├── Middleware/
│   ├── GlobalExceptionMiddleware.cs
│   ├── CorrelationIdMiddleware.cs
│   ├── IdempotencyMiddleware.cs
│   └── RequestLoggingMiddleware.cs
│
├── Authentication/
│   ├── JwtBearerSetup.cs
│   ├── AuthorizationPolicies.cs
│   └── PermissionRequirement.cs
│
├── OpenApi/
│   ├── SwaggerSetup.cs
│   └── AuthorizeOperationFilter.cs
│
├── HealthChecks/
│   ├── HealthChecksSetup.cs
│   └── DatabaseHealthCheck.cs
│
└── DependencyInjection.cs
```

### Rules

- **Minimal APIs preferred.** Controllers allowed only when the framework feature requires them (e.g. legacy file uploads, model binders).
- **One endpoint group per aggregate**, registered as `app.Map<Aggregate>Endpoints()`.
- **Endpoints are thin**: parse → `mediator.Send(...)` → return `Results.Ok/Created/NoContent`.
- **All errors flow through `GlobalExceptionMiddleware`** → RFC 9457 `ProblemDetails`.
- **`Program.cs` stays under ~80 lines.** Setup methods extract to extension classes.

---

## 7. Shared Project — `<Project>.Shared` (optional)

For truly cross-cutting primitives that don't belong to any one layer:

```
<Project>.Shared/
├── Guards/
│   └── Guard.cs                                # Guard.AgainstNull, Guard.AgainstEmpty
├── Results/
│   └── Result.cs                               # Result<T, E>
├── Pagination/
│   ├── PageRequest.cs
│   └── PagedList.cs
├── Constants/
│   └── ApplicationConstants.cs
└── Extensions/
    └── StringExtensions.cs
```

### Rules

- **Zero project references** other than `Microsoft.Extensions.*` if needed.
- **Add to `Shared` only** when used by 2+ layers. Otherwise put it in the layer that uses it.

---

## 8. Workers Project — `<Project>.Workers` (optional)

```
<Project>.Workers/
├── Program.cs
├── Workers/
│   ├── OutboxProcessorWorker.cs
│   ├── <Domain>ExpiryWorker.cs
│   └── <Event>ConsumerWorker.cs
└── DependencyInjection.cs
```

### Rules

- **Each worker is a `BackgroundService`.**
- **Long-running workers use `IServiceScopeFactory`** to create scoped services per iteration.
- **Workers reference `Application` and `Infrastructure`**, not `Api`.

---

## 9. Test Projects

Tests mirror `src/` 1:1.

```
tests/
├── <Project>.Domain.UnitTests/
│   └── <Aggregate>s/
│       └── <Aggregate>Tests.cs
│
├── <Project>.Application.UnitTests/
│   ├── <Aggregate>s/
│   │   └── Commands/
│   │       └── Create<Aggregate>/
│   │           └── Create<Aggregate>CommandHandlerTests.cs
│   ├── Common/
│   │   └── Builders/                           # Test data builders
│   └── Fixtures/
│
├── <Project>.Infrastructure.IntegrationTests/
│   ├── Fixtures/
│   │   └── PostgresContainerFixture.cs         # Testcontainers
│   └── Persistence/
│       └── <Aggregate>RepositoryTests.cs
│
└── <Project>.Api.IntegrationTests/
    ├── Fixtures/
    │   └── ApiFactory.cs                       # WebApplicationFactory
    └── <Aggregate>s/
        └── <Aggregate>EndpointsTests.cs
```

### Rules

- **Test naming**: `{ClassUnderTest}Tests.cs`.
- **Test method naming**: `MethodName_Scenario_ExpectedResult`.
- **Integration tests use Testcontainers** for real PostgreSQL/Redis/RabbitMQ — never in-memory fakes for the data layer.
- **Reset DB between tests** with Respawn.

---

## 10. Reference Direction (Allowed Imports)

```
Api / Workers   ──►  Application  ──►  Domain
       │                  ▲
       └──►  Infrastructure  ──┘

Shared          ◄── used by any layer (no upward references)
```

- `Domain` references **nothing** (except `Shared` if needed).
- `Application` references **only** `Domain` (+ `Shared`).
- `Infrastructure` references `Domain` + `Application` (+ `Shared`).
- `Api` and `Workers` reference all three (+ `Shared`).

Enforced via `.csproj` and validated with **NetArchTest** in `Architecture.Tests`.

---

## 11. Modular Monolith (When Multiple Bounded Contexts)

```
src/
├── Modules/
│   ├── <ContextA>/
│   │   ├── <Project>.<ContextA>.Domain/
│   │   ├── <Project>.<ContextA>.Application/
│   │   ├── <Project>.<ContextA>.Infrastructure/
│   │   └── <Project>.<ContextA>.Api/           # Endpoint registration only
│   └── <ContextB>/
│       └── ...
├── Shared/
│   ├── <Project>.Shared.Kernel/                # Truly shared abstractions
│   └── <Project>.Shared.Infrastructure/        # Cross-cutting (logging, OTel)
└── Host/
    └── <Project>.Host.Api/                     # Composition root
```

**Rule:** Modules communicate only via integration events on a message bus, or via explicit public contracts. **No direct cross-module class references.** Each module owns its database schema.

---

## 12. New-Solution Bootstrap Commands

```bash
# Create solution
dotnet new sln -n <Project>

# Create projects
dotnet new classlib -n <Project>.Domain        -o src/<Project>.Domain
dotnet new classlib -n <Project>.Application   -o src/<Project>.Application
dotnet new classlib -n <Project>.Infrastructure -o src/<Project>.Infrastructure
dotnet new web      -n <Project>.Api           -o src/<Project>.Api
dotnet new classlib -n <Project>.Shared        -o src/<Project>.Shared

# Test projects
dotnet new xunit -n <Project>.Domain.UnitTests        -o tests/<Project>.Domain.UnitTests
dotnet new xunit -n <Project>.Application.UnitTests   -o tests/<Project>.Application.UnitTests
dotnet new xunit -n <Project>.Infrastructure.IntegrationTests -o tests/<Project>.Infrastructure.IntegrationTests
dotnet new xunit -n <Project>.Api.IntegrationTests    -o tests/<Project>.Api.IntegrationTests

# Wire references (run from solution root)
dotnet add src/<Project>.Application      reference src/<Project>.Domain
dotnet add src/<Project>.Infrastructure   reference src/<Project>.Application
dotnet add src/<Project>.Api              reference src/<Project>.Infrastructure src/<Project>.Application

# Add all to solution
dotnet sln add src/**/*.csproj tests/**/*.csproj
```
