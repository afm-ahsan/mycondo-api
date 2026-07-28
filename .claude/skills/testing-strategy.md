---
name: testing-strategy
description: MyCondo backend test project layout and what's actually covered vs. placeholder as of Wave 0. Use when adding tests or evaluating whether "tests pass" means anything for a given change.
---

# Backend Testing Strategy

## Projects (all exist, all currently placeholder-only as of 2026-07-28)

- `MyCondo.Domain.UnitTests` — domain invariants, value objects, aggregate behavior. No EF Core, no
  mocking frameworks needed for pure domain logic.
- `MyCondo.Application.UnitTests` — command/query handler logic with `NSubstitute` for dependencies.
- `MyCondo.Infrastructure.IntegrationTests` — EF Core configurations, repositories, interceptors
  against a real (or Testcontainers) PostgreSQL — `Testcontainers.PostgreSql` is listed as
  "added when first DB integration test lands" in `Directory.Packages.props` but not yet added.
- `MyCondo.Api.IntegrationTests` — `Microsoft.AspNetCore.Mvc.Testing`-based, full pipeline through
  HTTP.
- `MyCondo.MultiTenancyTests` — cross-tenant access must fail. See `postgresql-rls.md` for what these
  need to cover once RLS policies exist.
- `MyCondo.ArchitectureTests` — `NetArchTest.Rules`-based structural rules: no cross-feature project
  references outside declared boundaries, every endpoint has `[RequirePermission]` or
  `[AllowAnonymous]`, Domain has zero dependency on EF Core/ASP.NET Core. **None of these rules are
  written yet** — the project only has the default placeholder test.

## Reality check before trusting a green build

`dotnet test` currently passes 6/6 — but every one of those 6 tests is the unmodified
`dotnet new xunit` template (`Test1` with an empty body). Green here means "the test projects compile
and xUnit can discover a test," not "the feature works." Don't cite "all tests pass" as evidence a
change is correct until real assertions exist for the code path you touched.

## Assertion library

`AwesomeAssertions` (MIT fork of FluentAssertions 7.x API) — same fluent API
(`result.Should().Be(...)`), different package reference. Not `FluentAssertions` (ADR-003, license).

## When adding a new feature

Write the test in the same PR as the feature, in the project matching the layer you touched. A new
Command needs at minimum: a handler unit test (happy path + at least one validation-failure path) and,
if it's tenant-scoped, a cross-tenant-isolation test once RLS exists for that table.
