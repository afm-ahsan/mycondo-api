---
name: dotnet-backend
description: MyCondo backend conventions — layering, CQRS via Mediator, validation, and the actual command/query pattern already in use. Use for any change inside mycondo-api/src or mycondo-api/tests.
---

# .NET Backend Conventions

## Layering

`Domain` (zero external deps — no EF Core, no ASP.NET Core) → `Application` (Mediator handlers,
FluentValidation, DTOs; depends only on Domain) → `Infrastructure` (EF Core, Redis, external
providers; implements Application/Domain interfaces) → `Api` (thin — routing, auth, dispatch,
response shaping; no business logic, no direct `DbContext` access).

## CQRS pattern (already established — follow it exactly)

Look at `src/MyCondo.Application/Features/Auth/Commands/Login/` for the canonical shape:
`LoginCommand.cs` (`sealed record ... : IRequest<TResult>`), `LoginCommandHandler.cs`,
`LoginCommandValidator.cs` (FluentValidation). Every command needs a validator. Every command/query
lives in its own use-case folder under `Features/<Feature>/{Commands,Queries}/<UseCase>/`.

We use `Mediator` (martinothamar/Mediator, MIT), not MediatR — see
`mycondo-docs/02-architecture/Architecture_Decision_Register.md` ADR-002. API differences that bite:
`IPipelineBehavior<TMessage,TResponse>` returns `ValueTask<TResponse>`, takes
`(TMessage message, MessageHandlerDelegate<TMessage,TResponse> next, CancellationToken ct)` — order
matters. `IPublisher` doesn't exist; use `IMediator`. Domain events do **not** go through `Mediator` —
they use the project's own `IDomainEventHandler<T>` + `IDomainEventDispatcher` (source generator
doesn't like open generics for notifications).

## Conventions to always follow

- Nullable reference types are on, warnings are errors — don't suppress with `#pragma` without a
  documented reason; fix the actual issue (see `mycondo-api/CLAUDE.md` "Always Do"/"Never Do").
- Strongly-typed IDs (`UserId`, not raw `Guid`) — see `src/MyCondo.Domain/Users/UserId.cs` for the
  pattern.
- `IClock` instead of `DateTime.UtcNow` in domain code.
- `Guid.CreateVersion7()` for aggregate IDs (via `IIdGenerator`/`GuidV7IdGenerator`).
- Structured logging only — `logger.LogInformation("... {Field}", value)`, never interpolated strings.
- Never `Task.Result`/`.Wait()`; always `await`. Never `dynamic`.
- Never inline `modelBuilder.Entity<T>()` — use `IEntityTypeConfiguration<T>` (see
  `src/MyCondo.Infrastructure/Persistence/Configurations/`).

## Known gap (fixed 2026-07-28)

Commands under `Features/Auth/Commands/{Login,Register,RefreshToken,Logout,ChangePassword}` were
implemented but had no HTTP endpoint — fixed in Wave 1 Slice 1, see `api-contracts.md` for the
current `MyCondo.Api/Endpoints/` shape.

## Pipeline behaviors — register them explicitly (bug found 2026-07-28, see ADR-012)

`Mediator`'s source generator does **not** auto-register open-generic `IPipelineBehavior<,>`
implementations, despite what an earlier comment in `MyCondo.Application/DependencyInjection.cs`
claimed. If you add a new pipeline behavior, it needs an explicit
`services.AddScoped(typeof(IPipelineBehavior<,>), typeof(YourBehavior<,>))` call there — otherwise it
silently never runs (this is exactly how `ValidationBehavior` went unregistered for an entire wave:
nothing ever called `ISender.Send(...)` over HTTP until the first real endpoint, so invalid requests
were reaching handlers/the database instead of failing with a 400).
