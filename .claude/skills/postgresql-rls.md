---
name: postgresql-rls
description: MyCondo's Row-Level Security and multi-tenancy approach — implemented, enforced, and how to extend it to new tenant-scoped tables. Use for any migration, DbContext, or tenant-context change.
---

# PostgreSQL RLS & Multi-Tenancy

## Current actual state (2026-07-28, Wave 1 Slice 5)

RLS is real, not aspirational, on 5 tables: `identity.users`, `roles`, `role_assignments`,
`refresh_tokens`, `role_permissions` — each has `ENABLE ROW LEVEL SECURITY`, `FORCE ROW LEVEL
SECURITY`, and a `rls_<table>_tenant_isolation` policy (migration `Enable_Tenant_Row_Level_Security`).

**The connecting role matters as much as the policy — this bit us once already.** Postgres
superusers and `BYPASSRLS` roles always bypass row security, `FORCE` or not. The original setup used
one Postgres role (`mycondo_app`) for everything, and because it was also the container's `initdb`
bootstrap role (`POSTGRES_USER`), it was a superuser — so RLS was silently doing nothing, anywhere,
until the first real Testcontainers run (`MyCondo.MultiTenancyTests`, once Docker was actually
available) caught it: cross-tenant reads leaked, and `WITH CHECK` didn't reject a wrong-tenant insert.
See the ADR recording this in `mycondo-docs` (follow-up to ADR-009).

Fixed via the two-role split `docs/kickoff.md` already named (Phase 1 naming): `mycondo_migrator`
(DDL/owner — the container's bootstrap role, used only for `dotnet ef database update`) and
`mycondo_app` (runtime — non-superuser, owns nothing, DML-only via `GRANT`/`ALTER DEFAULT PRIVILEGES`
in migration `Grant_App_Role_Runtime_Privileges`). Since `mycondo_app` doesn't own these tables, RLS
now applies to it even without `FORCE` — `FORCE` stays in the migration anyway as explicit
defense-in-depth. **If you ever see the app connecting as a role that owns its own tables or is a
superuser, RLS is not protecting anything, no matter what the migration history says** — verify the
connecting role's `rolsuper`/`rolbypassrls` flags directly if in doubt, don't trust `ENABLE`/`FORCE`
alone.

**Not every tenant-related table has (or needs) `tenant_id`:** `identity.permissions` is global
reference data (same catalogue across all tenants) — correctly has no `tenant_id`, no RLS.
`tenancy.tenants` is the tenant root itself — also correctly has neither.

## How tenant context reaches the database

`TenantContextConnectionInterceptor` (`src/MyCondo.Infrastructure/Persistence/Interceptors/`) sets
`app.current_tenant_id` via `set_config` on **every** `ConnectionOpened`/`ConnectionOpenedAsync` —
reads and writes alike. This replaced an earlier approach that only set it in
`MyCondoDbContext.SaveChangesAsync` (writes only), which would have made reads silently return zero
rows once RLS was enabled. The interceptor **always** calls `set_config`, using `''` when there's no
current tenant — it never skips the call. Skipping would let a stale tenant value survive on a pooled
connection into an unrelated request (Npgsql connection pooling reuses physical connections across
`DbContext` instances; `ConnectionOpened` fires on every logical open regardless).

## Policy shape — copy this exactly for any new tenant-scoped table

```sql
ALTER TABLE <schema>.<table> ENABLE ROW LEVEL SECURITY;
ALTER TABLE <schema>.<table> FORCE ROW LEVEL SECURITY;
CREATE POLICY rls_<table>_tenant_isolation ON <schema>.<table>
  USING (tenant_id = NULLIF(current_setting('app.current_tenant_id', true), '')::uuid)
  WITH CHECK (tenant_id = NULLIF(current_setting('app.current_tenant_id', true), '')::uuid);
```

**Do not write `tenant_id = current_setting('app.current_tenant_id')::uuid` without the `NULLIF`.**
The connection interceptor sets the GUC to `''` for anonymous requests, and `''::uuid` raises a
Postgres error rather than evaluating to NULL — a naive cast turns every anonymous RLS-protected query
into a 500 instead of a safe empty result. `NULLIF(..., '')` collapses both "never set" and
"explicitly cleared" to NULL first, so the comparison itself evaluates to NULL (no visible rows).

Include `WITH CHECK` explicitly (don't rely on `USING` alone covering writes) — it's what rejects an
`INSERT`/`UPDATE` for the wrong tenant with a clear RLS-violation error instead of silently succeeding.

## Anonymous, tenant-targeted requests (Login, Register, RefreshToken)

**RLS breaks these unless you think about tenant context explicitly — this already happened once**
(ADR-013). These endpoints are `AllowAnonymous`: there's no JWT yet, so `ICurrentUserProvider.TenantId`
is null, so `TenantContextConnectionInterceptor` sets the GUC to `''`. Once RLS is on, that means
`Register`'s insert gets rejected by `WITH CHECK`, and `Login`'s query always returns zero rows —
looking exactly like "wrong password," not an infrastructure bug.

The fix already in place: `TenantContextAccessor` falls back to a value stashed in
`HttpContext.Items[TenantContextAccessor.RequestedTenantItemKey]`, which `AuthEndpoints.cs` sets from
the `TenantId` already present in the Login/Register/Refresh request body, before dispatching. If you
add a new anonymous, tenant-scoped endpoint (e.g. a future public tenant-signup flow), you must do the
same — set that item explicitly from whatever the request declares as its target tenant. Do not assume
an anonymous request against a tenant-scoped table "just works" — trace whether RLS has anything to
compare against before shipping it.

If the request has no explicit tenant to declare (like a refresh-token lookup by hash, which is
tenant-agnostic by nature — a random secret, not a tenant-scoped key), add the tenant as an explicit
field on the command instead (see `RefreshTokenCommand` for the pattern) and verify it independently in
the handler as defense-in-depth, since RLS's `USING` clause has nothing to filter that particular
lookup by.

## Adding RLS to a new tenant-scoped table

1. Confirm the table actually has `tenant_id` (add it in its own migration first if not — see
   `role_permissions`' `Add_TenantId_To_RolePermissions` migration for the pattern when retrofitting).
2. Add the table name to `TenantScopedTables` in a new migration modeled on
   `Enable_Tenant_Row_Level_Security` (or extend that array in a follow-up migration).
3. Add a cross-tenant test in `MyCondo.MultiTenancyTests` before/alongside enabling the policy — don't
   assume it works.

## Testing

`MyCondo.MultiTenancyTests` uses a real Testcontainers-backed Postgres (`MultiTenancyPostgresFixture`)
and constructs `MyCondoDbContext` directly with a settable `TestTenantContextAccessor` — no HTTP
involved, this project is specifically about DB-level isolation. Covers: cross-tenant read isolation,
`WITH CHECK` rejecting a wrong-tenant insert, and `pg_class` verification that `relrowsecurity`/
`relforcerowsecurity` are actually set on every expected table. Migrations run as `mycondo_migrator`
(bootstrap role); every context the tests actually assert against (`CreateDbContext`) connects as the
restricted `mycondo_app` role — using the bootstrap role for both, as the fixture originally did, made
these tests pass without RLS doing anything. Executed against a real Docker daemon, all 3 passing.

`MyCondo.Api.IntegrationTests`' `PostgresApiFactory` follows the same two-role pattern: migrates as
`mycondo_migrator`, but the actual `TestServer`/HTTP pipeline (`Services`, and therefore every request
a test sends) runs as `mycondo_app`.
