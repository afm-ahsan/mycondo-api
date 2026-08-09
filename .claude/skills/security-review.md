---
name: security-review
description: Security checklist specific to MyCondo's actual current gaps — uncommitted auth code, unenforced permissions, no RLS yet, dependency vulnerability gating. Use before merging anything touching auth, tenancy, or dependencies.
---

# Security Review Checklist

## Known gaps as of 2026-07-28 (don't assume these are handled)

- RLS **is enabled and forced** on the 5 tenant-scoped identity tables — see `postgresql-rls.md` for
  the mechanism and the exact policy shape to copy for any new tenant-scoped table you add. Don't
  assume a *new* table is protected just because RLS exists elsewhere — you must add its `tenant_id`
  column, its own `rls_<table>_tenant_isolation` policy, and a cross-tenant test yourself; RLS is
  per-table, not automatic.
- `RequirePermission(...)` enforcement **does exist** (`MyCondo.Api/Authorization/`, ADR-011) — use it
  on every new endpoint. It currently reads permission claims embedded in the JWT at login/refresh
  time, not a per-request server-side lookup; that's a documented, deliberate scope decision (ID-4),
  not a shortcut. The permission *catalogue* isn't seeded yet (ID-2) — `tenant.manage`-gated endpoints
  exist but no user can hold that permission yet.
- The Auth/Identity feature (JWT issuance, Argon2id hashing, refresh-token rotation) is implemented
  and committed (ADR-008 resolved) — safe to build on top of.

## Dependency hygiene

`Directory.Packages.props` uses central package management with `CentralPackageTransitivePinningEnabled`.
Run `dotnet list package --vulnerable --include-transitive` before merging any dependency bump.
NuGet restore already fails the build on `NU1903` (high-severity advisories) via
`TreatWarningsAsErrors` — if restore fails on a new advisory, pin the offending transitive package to
a patched version via an explicit `<PackageVersion>` entry (see the `Microsoft.OpenApi` pin added in
Wave 0 for the pattern) rather than suppressing the warning.

## Secrets

Never commit real/production secrets. Use GitHub Secrets / AWS Secrets Manager in CI/prod, `MYCONDO_*`
env var prefix. `appsettings.json` contains no connection string at all by design (no placeholder to
mistake for real); `appsettings.Development.json` **does** contain a real, development-only PostgreSQL
connection string (`Host=localhost;...;Username=dev_user;Password=PgDev@1357#`) — this is a deliberate,
temporary MVP exception (mycondo-docs ADR-023, "Temporary MVP Development Credential Strategy"), not
the target architecture, and must never be treated as a template for Staging/Production config. That
credential must never be reused anywhere else and must be rotated (not just deleted from config)
before this repo is used against a shared/production database. `dotnet user-secrets` is not required
for local dev under this MVP decision, but remains available as an optional personal override.

## Platform-admin bypass

If/when a platform-admin bypass of tenant filtering is built (for support/ops tooling), it must be
explicit, narrowly scoped, and audited — never a generic "disable tenant filter" method reachable from
normal request handling.
