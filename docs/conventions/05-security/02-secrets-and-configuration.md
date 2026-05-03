# Secrets and Configuration

This document covers how secrets and configuration values are stored, accessed, and rotated. **No secret is ever committed to Git.**

---

## 1. Configuration Sources (in precedence order)

```
1. Command-line arguments       (highest)
2. Environment variables
3. User secrets (development)
4. Provider-specific stores     (Azure Key Vault, AWS Secrets Manager)
5. appsettings.{Environment}.json
6. appsettings.json             (lowest)
```

The .NET configuration system merges these. Later sources override earlier ones.

---

## 2. What Goes Where

| Setting type                        | Storage                                                  |
|-------------------------------------|----------------------------------------------------------|
| Public app config (port, app name)  | `appsettings.json` (committed)                           |
| Per-environment overrides           | `appsettings.Development.json`, `.Staging.json`, etc.    |
| Local dev secrets                   | `dotnet user-secrets`                                    |
| Production secrets                  | Azure Key Vault / AWS Secrets Manager / GitHub Secrets   |
| CI/CD secrets                       | GitHub Actions secrets (encrypted)                       |
| Frontend public config              | `VITE_*` env vars (visible in browser bundle)            |

### Rules

- **`appsettings.json` is committed.** It contains structure + safe defaults. No secrets.
- **`appsettings.{Environment}.json`** is committed. Per-env values, no secrets.
- **`appsettings.Development.json`** is committed but contains only local-dev values (e.g. `localhost`).
- **`appsettings.local.json` is gitignored** (optional, for ad-hoc overrides).
- **`.env` is gitignored.** `.env.example` is committed.

---

## 3. `appsettings.json` Shape

```jsonc
{
  "Logging": { "LogLevel": { "Default": "Information" } },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "Default": "Host=...;Database=...;Username=...;Password=...;",
    "Redis": "localhost:6379"
  },
  "Jwt": {
    "Issuer": "<Project>",
    "Audience": "<Project>.Web",
    "AccessTokenMinutes": 15,
    "RefreshTokenDays": 7
  },
  "Cors": {
    "AllowedOrigins": [ "http://localhost:5173" ]
  },
  "Swagger": {
    "Enabled": true
  }
}
```

### Rules

- **Connection strings have placeholder values** in committed files — real values come from secrets.
- **Secrets like `Jwt:SigningKey` are absent** in committed files. They must be present in user-secrets or production secret stores.
- **Strict shape**: production startup fails if a required secret is missing (`ValidateOnStart()`).

---

## 4. User Secrets (Local Development)

```bash
# Initialize once per project
dotnet user-secrets init --project src/<Project>.Api

# Set values
dotnet user-secrets set --project src/<Project>.Api \
  "Jwt:SigningKey" "your-32-char-or-longer-signing-key"

dotnet user-secrets set --project src/<Project>.Api \
  "ConnectionStrings:Default" "Host=localhost;Database=<project>_dev;..."

# List
dotnet user-secrets list --project src/<Project>.Api
```

User secrets live in `%APPDATA%\Microsoft\UserSecrets\<id>\secrets.json` on Windows; `~/.microsoft/usersecrets/<id>/secrets.json` on Linux/Mac. Outside the repo.

### Rules

- **Every developer runs `dotnet user-secrets init`** once per repo.
- **README documents** what user-secrets must be set for local dev (without revealing values).
- **CI doesn't use user secrets.** It uses environment variables / GitHub Secrets.

---

## 5. Environment Variables

In production, settings flow in via environment variables. .NET reads them with `:` replaced by `__`:

```
Jwt__SigningKey=...
ConnectionStrings__Default=...
ConnectionStrings__Redis=...
```

### Rules

- **Environment-variable names use double underscore** (`__`) for nesting.
- **Variables documented** in `.env.example` (frontend) and the deployment runbook (backend).

---

## 6. Frontend Environment Variables

`.env.example` (committed):

```
VITE_API_BASE_URL=http://localhost:5000
VITE_APP_NAME=<Project>
VITE_APP_VERSION=0.1.0
VITE_ENV=development
```

`.env` (gitignored):

```
VITE_API_BASE_URL=https://api.example.com
```

### Rules

- **Only `VITE_*` env vars** are exposed to the browser bundle.
- **Browser env vars are public.** Anyone with the bundle can read them. **Never put secrets here.**
- **Validated at startup** via Zod (`src/lib/env.ts`).

---

## 7. Production Secret Stores

### Azure Key Vault

```csharp
if (builder.Environment.IsProduction())
{
    var keyVaultUrl = builder.Configuration["KeyVault:Url"];
    builder.Configuration.AddAzureKeyVault(
        new Uri(keyVaultUrl!),
        new DefaultAzureCredential());
}
```

### AWS Secrets Manager

Use `Amazon.Extensions.Configuration.SystemsManager` or pull secrets at boot via the AWS SDK.

### Local Docker Secrets

```yaml
# docker-compose.yml
services:
  api:
    secrets:
      - jwt_signing_key
secrets:
  jwt_signing_key:
    file: ./secrets/jwt_signing_key.txt
```

### Rules

- **Use managed identity / IRSA where available.** No service-account passwords.
- **Rotate signing keys quarterly** (or on suspected compromise).
- **Audit access** to secret stores.

---

## 8. Strongly-Typed Settings with Validation

Define a settings class per section with data annotations:

```csharp
public sealed record JwtSettings
{
    [Required] public string Issuer { get; init; } = default!;
    [Required] public string Audience { get; init; } = default!;
    [Required, MinLength(32)] public string SigningKey { get; init; } = default!;
    [Range(1, 60)] public int AccessTokenMinutes { get; init; } = 15;
    [Range(1, 30)] public int RefreshTokenDays { get; init; } = 7;
}

services.AddOptions<JwtSettings>()
        .BindConfiguration("Jwt")
        .ValidateDataAnnotations()
        .ValidateOnStart();
```

### Rules

- **One settings class per section**, named `<Section>Settings`.
- **Records with `init` setters.**
- **`ValidateDataAnnotations()` + `ValidateOnStart()`** so a missing/invalid secret fails the app at boot, not at first use.
- **Inject `IOptions<TSettings>`** in services. Don't read `IConfiguration["..."]` directly in business code.

---

## 9. Secret Rotation

| Secret                | Rotation cadence       | Process                                                 |
|-----------------------|------------------------|---------------------------------------------------------|
| JWT signing key       | Quarterly + on incident| Roll keys; support both old and new for the access TTL window |
| DB password           | Quarterly + on incident| Update via cloud provider; `kubectl rollout restart`    |
| API keys (3rd-party)  | Per-vendor policy      | Update in secret store; restart                         |
| Encryption keys       | Annually + on incident | Re-encrypt or use envelope encryption                   |

### Rules

- **Keys rotate without downtime** by accepting both old and new keys during overlap.
- **Document rotation procedure per secret** in `docs/runbooks/`.
- **Test rotation in staging** before performing in production.

---

## 10. Pre-Commit Secret Scan

Add a pre-commit hook (or use GitHub's secret scanning):

```bash
# .git/hooks/pre-commit (sketch)
git diff --cached | grep -E '(SigningKey|password|api_?key|secret)\s*=\s*["'\'']?[A-Za-z0-9]{12,}' \
  && { echo "Possible secret in commit"; exit 1; } || exit 0
```

Tools: **gitleaks**, **trufflehog**, **GitHub secret scanning** (free for public repos).

### Rules

- **Pre-commit secret scan in every repo.**
- **GitHub secret scanning enabled.**
- **If a secret leaks**: rotate immediately, then deal with the commit history.

---

## 11. Configuration in Tests

Integration tests use `WebApplicationFactory.ConfigureAppConfiguration`:

```csharp
builder.ConfigureAppConfiguration((_, cfg) =>
{
    cfg.AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["ConnectionStrings:Default"] = postgresContainer.GetConnectionString(),
        ["ConnectionStrings:Redis"] = redisContainer.GetConnectionString(),
        ["Jwt:SigningKey"] = "test-signing-key-32-chars-minimum-length",
        ["Jwt:Issuer"] = "test",
        ["Jwt:Audience"] = "test"
    });
});
```

### Rules

- **Tests provide all required config.** No reliance on the developer's environment.
- **Test signing keys are not real.** They're just long enough to satisfy validation.

---

## 12. Common Mistakes

| Mistake                                                          | Fix                                                              |
|------------------------------------------------------------------|------------------------------------------------------------------|
| Connection string committed in `appsettings.json`                | Placeholder; real value via user-secrets / env var               |
| Reading `IConfiguration["Jwt:SigningKey"]` in business code      | `IOptions<JwtSettings>`                                          |
| `Console.WriteLine(jwtSigningKey)` for "debugging"                | Never log secrets                                               |
| Same JWT signing key across environments                         | Per-environment, generated independently                         |
| Accidentally committing `.env`                                   | Pre-commit hook + `.gitignore`                                   |
| `VITE_DATABASE_PASSWORD` in frontend                             | Frontend env vars are public — no secrets there                  |
| Settings class without `[Required]`                              | App boots with empty secrets; fails at runtime                   |
| Rotating a secret without overlap window                         | Brief outage as in-flight tokens become invalid                  |
| Missing `ValidateOnStart()`                                      | Bad config caught at first request, not at boot                  |
