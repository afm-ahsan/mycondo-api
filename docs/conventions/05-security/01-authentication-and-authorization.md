# Authentication and Authorization

The authentication model is **JWT bearer tokens with refresh tokens**. The authorization model is **role-based with permission claims** (RBAC + permissions).

---

## 1. Token Strategy

```
┌─────────────────────────────────────────────────────────┐
│  Login                                                   │
│   ── access token (JWT, ~15 min, in memory)             │
│   ── refresh token (opaque, ~7 days, HTTP-only cookie)  │
└─────────────────────────────────────────────────────────┘
            │
            ▼
┌─────────────────────────────────────────────────────────┐
│  Every request                                           │
│   Authorization: Bearer <access>                         │
└─────────────────────────────────────────────────────────┘
            │
            ▼
┌─────────────────────────────────────────────────────────┐
│  Access expires                                          │
│   POST /api/auth/refresh (cookie sent automatically)    │
│   Server rotates: new access + new refresh              │
└─────────────────────────────────────────────────────────┘
            │
            ▼
┌─────────────────────────────────────────────────────────┐
│  Logout                                                  │
│   POST /api/auth/logout                                  │
│   Server revokes refresh; clears cookie                 │
└─────────────────────────────────────────────────────────┘
```

### Rules

- **Access tokens (JWT)**: short-lived (~15 minutes), in browser **memory** (Redux store), never in `localStorage`.
- **Refresh tokens**: long-lived (~7 days), **HTTP-only cookie** (`SameSite=Strict`, `Secure`, `Path=/api/auth`). The browser handles them; JS never sees them.
- **Refresh tokens are rotating**: each refresh issues a new refresh token and invalidates the previous one.
- **Refresh token reuse detected** → all of the user's tokens are revoked.

---

## 2. JWT Settings

```jsonc
// appsettings.json
{
  "Jwt": {
    "Issuer": "<Project>",
    "Audience": "<Project>.Web",
    "SigningKey": "<32+ char secret>",
    "AccessTokenMinutes": 15,
    "RefreshTokenDays": 7
  }
}
```

```csharp
public sealed record JwtSettings
{
    [Required] public string Issuer { get; init; } = default!;
    [Required] public string Audience { get; init; } = default!;
    [Required, MinLength(32)] public string SigningKey { get; init; } = default!;
    [Range(1, 60)] public int AccessTokenMinutes { get; init; } = 15;
    [Range(1, 30)] public int RefreshTokenDays { get; init; } = 7;
}
```

### Rules

- **Signing key minimum 32 characters.** Validate at startup.
- **HS256** for single-deployment apps. **RS256** for public APIs / multi-service.
- **Validate every parameter** — issuer, audience, lifetime, signing key.
- **`ClockSkew = TimeSpan.FromMinutes(1)`** — defaults to 5 minutes which is too lax.

---

## 3. JWT Claims

Every access token carries:

| Claim                    | Source                                         |
|--------------------------|------------------------------------------------|
| `sub` (subject)          | `User.Id` (UUID)                               |
| `email`                  | `User.Email`                                   |
| `jti` (JWT ID)           | UUIDv7 — unique per token                      |
| `iat` (issued at)        | Server clock                                   |
| `exp` (expires at)       | iat + access TTL                               |
| `role` (zero or more)    | `User.Roles` — names                           |
| `perm` (zero or more)    | All permissions granted by the user's roles    |
| `tenant` (optional)      | `User.TenantId` (multi-tenant apps)            |

### Rules

- **Permissions are flattened into claims** at login. Frontend reads `perm` claims to show/hide UI.
- **Backend authorizes against `perm` claims**, not against database lookups per request.
- **No PII in the token** beyond `email`. No phone, no name, no address.

---

## 4. Issuing Tokens

```csharp
public sealed class JwtTokenService(
    IOptions<JwtSettings> settings,
    IClock clock
) : IJwtTokenService
{
    public AccessToken GenerateAccessToken(User user, IReadOnlyCollection<string> roles, IReadOnlyCollection<string> permissions)
    {
        var s = settings.Value;
        var nowUtc = clock.UtcNow.UtcDateTime;
        var jti = Guid.CreateVersion7().ToString();

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.Value.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Jti, jti),
            new(JwtRegisteredClaimNames.Iat, nowUtc.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));
        claims.AddRange(permissions.Select(p => new Claim("perm", p)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(s.SigningKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var expires = nowUtc.AddMinutes(s.AccessTokenMinutes);
        var token = new JwtSecurityToken(s.Issuer, s.Audience, claims, expires: expires, signingCredentials: creds);
        var jwt = new JwtSecurityTokenHandler().WriteToken(token);

        return new AccessToken(jwt, expires, jti);
    }
}
```

---

## 5. Refresh Token Storage

Refresh tokens are stored server-side so they can be revoked:

```csharp
public sealed class RefreshToken : Entity<RefreshTokenId>, IAuditable
{
    public UserId UserId { get; private set; }
    public string TokenHash { get; private set; }    // BCrypt hash of the raw token
    public DateTimeOffset ExpiresAtUtc { get; private set; }
    public DateTimeOffset? RevokedAtUtc { get; private set; }
    public RefreshTokenId? ReplacedById { get; private set; }   // For rotation chains
    // ... audit columns ...

    public bool IsActive => RevokedAtUtc is null && DateTimeOffset.UtcNow < ExpiresAtUtc;
}
```

### Rules

- **Store a hash**, never the raw token.
- **Track replacement** so reuse can be detected.
- **Index on `(user_id, token_hash)`** for fast lookup.

---

## 6. Authentication Endpoints

```csharp
// /api/auth/login
public sealed record LoginRequest(string Email, string Password);

public sealed record LoginResponse(string AccessToken, AuthUserDto User, DateTimeOffset ExpiresAtUtc);

// /api/auth/refresh   (refresh cookie sent automatically)
// Returns: { accessToken, user, expiresAtUtc }

// /api/auth/logout    (refresh cookie sent automatically)
// Server revokes refresh; clears cookie.

// /api/auth/me        (returns current user)
```

### Rate-limit auth endpoints harder

```csharp
group.MapPost("/login", LoginAsync)
     .RequireRateLimiting("AuthPolicy");   // 5/minute per IP
```

---

## 7. Login Flow (Code)

```csharp
public sealed class LoginCommandHandler(
    IUserRepository users,
    IPasswordHasher hasher,
    IJwtTokenService jwt,
    IRefreshTokenService refresh,
    ICookieWriter cookies,
    IClock clock
) : IRequestHandler<LoginCommand, LoginResponse>
{
    public async Task<LoginResponse> Handle(LoginCommand command, CancellationToken ct)
    {
        var user = await users.GetByEmailAsync(command.Email, ct);
        if (user is null || !hasher.Verify(command.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid credentials.");

        if (!user.IsActive)
            throw new ForbiddenException("Account is disabled.");

        var (roles, perms) = await users.GetRolesAndPermissionsAsync(user.Id, ct);

        var access = jwt.GenerateAccessToken(user, roles, perms);
        var refreshTok = await refresh.IssueAsync(user.Id, ct);

        cookies.WriteRefreshToken(refreshTok.Raw, refreshTok.ExpiresAtUtc);

        return new LoginResponse(access.Token, user.ToDto(roles, perms), access.ExpiresAtUtc);
    }
}
```

### Rules

- **BCrypt for password verification** (`BCrypt.Net-Next`).
- **Generic error message** ("Invalid credentials") whether email or password is wrong — don't leak account existence.
- **Inactive accounts return 403**, not 401.
- **Refresh token is the only thing in the cookie.** No JWT in cookies.

---

## 8. Authorization in the API

### Default deny

```csharp
group.RequireAuthorization();   // applied to every endpoint group
```

Public endpoints opt out:

```csharp
group.MapPost("/login", LoginAsync).AllowAnonymous();
```

### Permission-based policies

```csharp
// Api/Authentication/AuthorizationPolicies.cs
public static class Permissions
{
    public const string CustomersView = "customers.view";
    public const string CustomersManage = "customers.manage";
    public const string ReportsView = "reports.view";
    public const string SystemAdmin = "system.admin";
}

services.AddAuthorization(options =>
{
    foreach (var perm in Permissions.All)
    {
        options.AddPolicy(perm, p =>
            p.RequireAuthenticatedUser().RequireClaim("perm", perm));
    }
});
```

Apply per endpoint:

```csharp
group.MapGet("/", ListAsync).RequireAuthorization(Permissions.CustomersView);
group.MapPost("/", CreateAsync).RequireAuthorization(Permissions.CustomersManage);
group.MapDelete("/{id:guid}", DeleteAsync).RequireAuthorization(Permissions.CustomersManage);
```

### Rules

- **One permission per concern.** `customers.view`, `customers.manage`, `customers.delete` if delete is special.
- **Permissions named `<resource>.<action>`** in dot notation.
- **System admin permission** (`system.admin`) implicitly grants everything — but check it explicitly only at sensitive endpoints.

---

## 9. Roles vs Permissions

```
User ─── n:m ─── Role ─── n:m ─── Permission
```

- **Permissions are atomic** ("can edit customer", "can view reports").
- **Roles are bundles of permissions** ("Admin", "Sales Rep", "Viewer").
- **Endpoints check permissions**, not roles — adding a new role doesn't require touching endpoint code.

### Schema (auth schema)

```sql
CREATE TABLE auth.user (
    id              uuid PRIMARY KEY,
    email           text NOT NULL,
    password_hash   text NOT NULL,
    is_active       boolean NOT NULL DEFAULT true,
    created_at_utc  timestamptz NOT NULL DEFAULT now(),
    -- ...
    CONSTRAINT uq_user_email UNIQUE (email)
);

CREATE TABLE auth.role (
    id    int GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    name  text NOT NULL,
    CONSTRAINT uq_role_name UNIQUE (name)
);

CREATE TABLE auth.permission (
    id    int GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    code  text NOT NULL,                   -- e.g. 'customers.view'
    CONSTRAINT uq_permission_code UNIQUE (code)
);

CREATE TABLE auth.user_role (
    user_id  uuid NOT NULL REFERENCES auth.user(id),
    role_id  int  NOT NULL REFERENCES auth.role(id),
    CONSTRAINT pk_user_role PRIMARY KEY (user_id, role_id)
);

CREATE TABLE auth.role_permission (
    role_id        int NOT NULL REFERENCES auth.role(id),
    permission_id  int NOT NULL REFERENCES auth.permission(id),
    CONSTRAINT pk_role_permission PRIMARY KEY (role_id, permission_id)
);
```

### Seed data

Roles and permissions are seeded at startup:

```csharp
public static class IdentitySeeder
{
    public static async Task SeedAsync(AppDbContext db, CancellationToken ct)
    {
        // Permissions
        var allPerms = Permissions.All.Select(code => new Permission(code)).ToList();
        foreach (var p in allPerms)
            if (!await db.Permissions.AnyAsync(x => x.Code == p.Code, ct))
                db.Permissions.Add(p);

        // Roles + their permissions
        await EnsureRoleAsync(db, "Admin", Permissions.All, ct);
        await EnsureRoleAsync(db, "Manager", new[] { Permissions.CustomersManage, Permissions.CustomersView, Permissions.ReportsView }, ct);
        await EnsureRoleAsync(db, "Viewer", new[] { Permissions.CustomersView, Permissions.ReportsView }, ct);

        await db.SaveChangesAsync(ct);
    }
}
```

---

## 10. Frontend — Permission Checks

```ts
// src/auth/use-permissions.ts
import { useAppSelector } from '@/store/hooks';

export function usePermissions() {
  const perms = useAppSelector(s => s.auth.user?.permissions ?? []);
  return {
    has: (perm: string) => perms.includes(perm),
    any: (...needed: string[]) => needed.some(p => perms.includes(p)),
    all: (...needed: string[]) => needed.every(p => perms.includes(p)),
  };
}

// In components:
const { has } = usePermissions();
if (has('customers.manage')) {
  return <Button onClick={create}>Add Customer</Button>;
}
```

### Route-level guard

```tsx
// src/auth/require-permission.tsx
import { Navigate, Outlet } from 'react-router-dom';
import { usePermissions } from './use-permissions';

export function RequirePermission({ permission }: { permission: string }) {
  const { has } = usePermissions();
  if (!has(permission)) return <Navigate to="/forbidden" replace />;
  return <Outlet />;
}
```

### Rules

- **Frontend permission checks are UX, not security.** The backend is the only authority.
- **Hide actions the user can't perform** — don't show a disabled button. Hide the button.

---

## 11. Account Security

### Password rules (validated server-side)

- Minimum 12 characters.
- At least one letter and one number.
- Not in the breached-passwords list (`HaveIBeenPwned` API, optional).

### Account lockout

- 5 failed logins in 15 minutes → account locked for 15 minutes (or until admin unlocks).
- Failed login attempts logged with IP and email.

### MFA (optional but recommended)

- TOTP via `Otp.NET`.
- Enrollment shows a QR code; backup codes stored encrypted.
- MFA required for admin role; optional for others.

### Session management

- Logout revokes the refresh token.
- "Logout all sessions" revokes every refresh token for the user.
- Password change revokes all refresh tokens.

---

## 12. CORS for Authenticated Apps

```csharp
options.AddPolicy("DefaultCors", builder =>
{
    builder
        .WithOrigins(configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()!)
        .AllowCredentials()                          // Required for cookies
        .AllowAnyHeader()
        .AllowAnyMethod()
        .WithExposedHeaders("X-Correlation-Id");
});
```

### Rules

- **Origin whitelist required** when `AllowCredentials()` is on (browsers reject `*`).
- **List origins per environment** — local dev origins are different from production.

---

## 13. Common Mistakes

| Mistake                                                          | Fix                                                              |
|------------------------------------------------------------------|------------------------------------------------------------------|
| Storing JWT in `localStorage`                                    | In-memory access token + HTTP-only refresh cookie                |
| Refresh token in `localStorage`                                  | HTTP-only cookie                                                 |
| Long-lived access tokens (hours/days)                            | 15 minutes                                                       |
| Endpoints check role names                                       | Check permission claims                                          |
| `Authorization: Bearer <jwt>` missing on requests                | RTK Query `prepareHeaders` adds it from store                    |
| 401 message reveals "no such user"                               | Generic "Invalid credentials"                                    |
| No rate limit on `/login`                                        | 5/minute per IP                                                  |
| Inactive user gets 401                                           | 403 Forbidden                                                    |
| Frontend permission as the only check                            | Backend is authoritative                                         |
| Refresh token rotation not implemented                           | Rotate; detect reuse                                             |
| Same JWT signing key in all environments                         | Per-environment keys; rotate on incident                         |
