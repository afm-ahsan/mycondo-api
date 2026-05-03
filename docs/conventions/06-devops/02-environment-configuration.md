# Environment Configuration

This document covers how environments are defined, what differs between them, and how the same image runs in each.

---

## 1. Environments

| Environment   | Purpose                                       | Audience                  |
|---------------|-----------------------------------------------|---------------------------|
| `Development` | Local dev (developer's machine)               | Engineers                 |
| `Test`        | Automated test runs                           | CI                        |
| `Staging`     | Pre-prod; mirrors prod config and data shape  | QA, internal stakeholders |
| `Production`  | Live system                                   | End users                 |

Set via `ASPNETCORE_ENVIRONMENT` (backend) and `VITE_ENV` (frontend).

---

## 2. What Differs Per Environment

| Setting                       | Development           | Staging              | Production                       |
|-------------------------------|-----------------------|----------------------|----------------------------------|
| Connection strings            | Local Docker          | Managed staging DB   | Managed production DB            |
| JWT signing key               | Dev fixed value       | Per-env vault secret | Per-env vault secret             |
| CORS origins                  | localhost             | staging frontend     | production frontend              |
| Swagger UI                    | Enabled               | Behind auth          | Disabled (or behind auth)        |
| Log level (default)           | `Information`         | `Information`        | `Warning`                        |
| Detailed errors               | On                    | Off                  | Off                              |
| Sensitive data logging (EF)   | On                    | Off                  | Off                              |
| Rate limit (per user)         | Generous              | Production-equivalent| Production                       |
| Telemetry exporter            | Console (or none)     | OTLP staging         | OTLP production                  |
| Backup schedule               | None                  | Daily                | Hourly                           |

---

## 3. `appsettings.{Environment}.json` Files

```
src/<Project>.Api/
├── appsettings.json                     # Base + safe defaults
├── appsettings.Development.json         # Local dev overrides (committed)
├── appsettings.Staging.json             # Staging overrides (committed)
├── appsettings.Production.json          # Production overrides (committed)
```

**No secrets** in any of these files — only structure and per-env switches.

```jsonc
// appsettings.Development.json
{
  "Logging": { "LogLevel": { "Default": "Information", "Microsoft": "Warning" } },
  "Cors": { "AllowedOrigins": ["http://localhost:5173"] },
  "Swagger": { "Enabled": true },
  "DetailedErrors": true
}

// appsettings.Production.json
{
  "Logging": { "LogLevel": { "Default": "Warning", "Microsoft.AspNetCore": "Warning" } },
  "Cors": { "AllowedOrigins": [] },           // populated via env var; empty default forces explicit config
  "Swagger": { "Enabled": false },
  "DetailedErrors": false
}
```

### Rules

- **All environment files are committed.** They're structure, not secrets.
- **Production file is conservative**: low log verbosity, errors hidden, Swagger off.
- **`AllowedOrigins` empty in production by default** — must be set explicitly via env var or vault. Forces a deliberate choice.

---

## 4. Frontend `.env.<environment>` Files

```
src/<Project>.Client/
├── .env.example          # committed; documents required vars
├── .env                  # gitignored; local dev values
├── .env.development      # committed; defaults for `vite dev`
├── .env.staging          # committed; build-time defaults for staging
├── .env.production       # committed; build-time defaults for production
```

```bash
# .env.production
VITE_API_BASE_URL=https://api.example.com
VITE_APP_NAME=<Project>
VITE_ENV=production
VITE_FEATURE_REPORTS=true
```

### Rules

- **Frontend env vars are public** (they ship in the bundle). No secrets.
- **`VITE_API_BASE_URL` differs per environment** — separate builds, or runtime config.
- **For runtime config (one bundle, many envs)**: serve a `/config.json` endpoint that the SPA fetches at boot. Useful for ops who want to switch backends without rebuilding.

---

## 5. Runtime Configuration (One Bundle, Many Envs)

If you want one frontend bundle that runs against any backend:

```ts
// src/lib/runtime-config.ts
type RuntimeConfig = { apiBaseUrl: string; env: string; featureReports: boolean };
let cached: RuntimeConfig | null = null;

export async function loadRuntimeConfig(): Promise<RuntimeConfig> {
  if (cached) return cached;
  const res = await fetch('/config.json', { cache: 'no-store' });
  cached = await res.json();
  return cached!;
}
```

`public/config.json` is replaced per-deploy (e.g. nginx mounts a configMap):

```json
{ "apiBaseUrl": "https://api.example.com", "env": "production", "featureReports": true }
```

### Rules

- **Use runtime config when the same bundle deploys to multiple environments.**
- **For most projects, build-time `.env.<env>` is simpler.**

---

## 6. Connection Strings — Production Patterns

```
ConnectionStrings__Default = "Host=db.internal;Database=app;Username=app;Password=...;SslMode=Require;"
ConnectionStrings__Redis   = "redis-prod.internal:6379,ssl=True,password=..."
```

### Rules

- **`SslMode=Require`** in production.
- **Pooling defaults are usually fine** (Npgsql 100 connections per pool). Adjust only after profiling.
- **Read replicas** (when used): a separate `ConnectionStrings__ReadOnly` for the read-side Dapper repos.
- **Managed identity** (Azure / AWS) preferred over username/password where supported.

---

## 7. Feature Flags

For risky launches, use a feature flag:

```csharp
public sealed record FeatureFlags
{
    public bool ReportsEnabled { get; init; }
    public bool NewBillingFlow { get; init; }
}

services.AddOptions<FeatureFlags>().BindConfiguration("Features").ValidateOnStart();

// Usage
if (flags.ReportsEnabled) {
    // show reports
}
```

### Rules

- **Boolean flags only** for simple cases; use a feature-flag service (Unleash, ConfigCat, LaunchDarkly) for percentage rollouts.
- **Flags are temporary.** Each flag has an expiration date in code or in an issue tracker.
- **Frontend reads flags** via the same backend config — through an `/api/config` endpoint or via runtime config.

---

## 8. Database Migrations Per Environment

```
Development → CI runs migrations against ephemeral Postgres (Testcontainers)
Staging     → Deploy job runs migrations before app rollout
Production  → Same; with backup taken first; idempotent script
```

### Rules

- **Migrations and code deploy as separate jobs.** Migration job runs first.
- **Migration script is idempotent** (`dotnet ef migrations script --idempotent`).
- **Backup before destructive migrations** (DROP, ALTER TYPE).
- **Never run migrations from the application's startup** in production for non-trivial systems — startup race conditions and partial failure modes are no fun.

---

## 9. Time Zones

- **Servers run in UTC.** No exceptions.
- **Database stores `timestamptz`.** Display conversion happens on the frontend.
- **Frontend stores ISO 8601 with Z** for UTC; uses `date-fns` to convert for display.

### Rules

- **Don't use `DateTime.Now`.** Use `DateTimeOffset.UtcNow` or `IClock.UtcNow`.
- **Test fixtures use a fixed instant.** Time-dependent tests are deterministic.

---

## 10. Locale and Internationalization

- **Backend default locale `en-US`** unless the app is single-locale-non-English.
- **`InvariantCulture`** for parsing/formatting numbers and dates in code; user-facing formatting honors the user's locale.
- **Frontend uses `react-intl` or `i18next`.** Translations in JSON per locale.

---

## 11. Observability Endpoints Per Env

| Endpoint                  | Development          | Staging              | Production           |
|---------------------------|----------------------|----------------------|----------------------|
| OTLP traces/metrics/logs  | `localhost:4317`     | staging collector    | production collector |
| Health (`/health/live`)   | Any                  | LB checks            | LB checks            |
| Health (`/health/ready`)  | Any                  | LB checks            | LB checks            |
| Metrics (`/metrics`)      | Any                  | Internal (auth'd)    | Internal (auth'd)    |
| Swagger (`/swagger`)      | Any                  | Internal (auth'd)    | Off (or auth'd)      |

---

## 12. Common Mistakes

| Mistake                                                   | Fix                                                                |
|-----------------------------------------------------------|--------------------------------------------------------------------|
| `appsettings.Production.json` with real connection string | Placeholder; real value via secret store / env var                 |
| Same JWT key across envs                                  | Per-env, generated independently                                   |
| Swagger reachable in production without auth              | Disable or wrap with `RequireAuthorization`                        |
| Server in non-UTC timezone                                 | Always UTC                                                         |
| Detailed errors on in production                          | Off                                                                |
| Migrations run from app startup in prod                   | Separate migration job                                             |
| CORS `*` in production                                    | Whitelist                                                          |
| Logs with default level `Trace` in production             | `Warning` or `Information` baseline                                |
| Feature flag without expiration                           | Add an issue / TODO tying it to removal                            |
