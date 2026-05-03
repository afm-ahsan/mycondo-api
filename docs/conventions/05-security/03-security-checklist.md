# Security Checklist

A pre-flight checklist applied to every feature, every PR, and every deployment. Mapped to **OWASP Top 10 (2021)**.

---

## 1. OWASP Top 10 Mapping

| Category                                    | How we address it                                                |
|---------------------------------------------|------------------------------------------------------------------|
| A01 Broken Access Control                   | Default deny; permission claims; backend authoritative           |
| A02 Cryptographic Failures                  | TLS everywhere; BCrypt for passwords; AES-GCM for column-level   |
| A03 Injection                               | Parameterized SQL; FluentValidation; Zod                         |
| A04 Insecure Design                         | Threat model per project; ADRs for security-affecting choices    |
| A05 Security Misconfiguration               | Settings validation at startup; Swagger off in prod              |
| A06 Vulnerable Components                   | Renovate / Dependabot; weekly review                             |
| A07 Identification & Authentication         | Short-lived JWT; rotating refresh; rate limit on auth            |
| A08 Software & Data Integrity               | Signed releases; CI artifacts only; dependency lockfiles         |
| A09 Logging & Monitoring                    | Structured logs; correlation IDs; alerts on auth failures        |
| A10 SSRF                                    | URL validation; outbound HTTP allow-list                         |

---

## 2. Per-PR Security Checklist

### Authentication & Authorization

- [ ] New endpoints `RequireAuthorization()` by default. Public endpoints opt-in via `AllowAnonymous`.
- [ ] New endpoints check the right permission claim.
- [ ] Frontend hides UI actions for users lacking permission (UX); backend re-checks (security).
- [ ] No role-name string checks in business logic — permissions only.

### Input Validation

- [ ] Every command has a FluentValidation validator.
- [ ] Every form has a Zod schema.
- [ ] String fields have `MaximumLength`.
- [ ] Numeric fields have ranges.
- [ ] Dates / times validated (`from <= to`, no future dates where impossible).

### Output Encoding

- [ ] No `dangerouslySetInnerHTML` (or DOMPurify if absolutely necessary).
- [ ] User-generated text is rendered as text, not HTML.
- [ ] URLs from user input pass `new URL(...)` validation before use as `<a href>` or `window.open`.

### Secrets & Config

- [ ] No secrets in code.
- [ ] No secrets in environment variables exposed to the browser (`VITE_*`).
- [ ] New config has `IOptions<T>` + `[Required]` annotations + `ValidateOnStart()`.

### Logging

- [ ] No logging of passwords, tokens, full credit cards, or full PII.
- [ ] Sensitive fields are redacted via the Serilog destructuring policy.
- [ ] Errors include the correlation ID.

### Database

- [ ] All SQL parameterized (Dapper/EF default; raw SQL via `FromSqlInterpolated` only).
- [ ] No unfiltered user input concatenated into SQL.
- [ ] New tables have audit columns and soft-delete (where appropriate).

### HTTP

- [ ] HTTPS only (`UseHttpsRedirection`).
- [ ] CORS origin whitelist (no `*` with credentials).
- [ ] Idempotency-Key on POST/DELETE.
- [ ] Rate limit on auth endpoints stricter than general.

### Dependencies

- [ ] No new dependency without an ADR if unusual.
- [ ] `npm audit` / `dotnet list package --vulnerable` clean.
- [ ] Lockfile committed.

---

## 3. Pre-Deployment Checklist

### Build & Release

- [ ] Migrations applied (idempotent script).
- [ ] DB backup taken before destructive migrations.
- [ ] Image signed and pushed to registry.
- [ ] Health checks return 200 from `/health/ready`.

### Security

- [ ] Production secrets present in vault (no missing settings).
- [ ] JWT signing key is environment-specific (not shared with staging).
- [ ] HTTPS certificate valid.
- [ ] HSTS enabled.
- [ ] Security headers set (see §5).
- [ ] CORS origins match production frontend URL only.
- [ ] Swagger UI disabled (or behind auth) in production.

### Observability

- [ ] OpenTelemetry exporter pointing at production OTLP endpoint.
- [ ] Logs flowing to log backend.
- [ ] Alerts wired (5xx rate, auth failures, health-check failures).
- [ ] Correlation ID flows end-to-end (test with a real request).

### Operational

- [ ] Runbook updated.
- [ ] Rollback plan documented.
- [ ] On-call engineer aware of release window.

---

## 4. Recurring Security Tasks

| Cadence       | Task                                                                                |
|---------------|-------------------------------------------------------------------------------------|
| Daily         | Review failed-login alerts; review 5xx logs                                          |
| Weekly        | Renovate / Dependabot PRs reviewed and merged or rejected                            |
| Monthly       | Review unused indexes; vacuum schema for unused permissions                          |
| Quarterly     | Rotate JWT signing key; rotate DB password; review user/role assignments             |
| Annually      | External pen test; re-validate RLS policies; renew TLS certificates if not auto      |

---

## 5. Security Headers

The reverse proxy (nginx) sets:

```nginx
add_header Strict-Transport-Security "max-age=31536000; includeSubDomains" always;
add_header X-Content-Type-Options "nosniff" always;
add_header X-Frame-Options "DENY" always;
add_header Referrer-Policy "strict-origin-when-cross-origin" always;
add_header Permissions-Policy "camera=(), microphone=(), geolocation=()" always;
add_header Content-Security-Policy "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'; img-src 'self' data: blob:; font-src 'self' data:; connect-src 'self' <api-host>;" always;
```

### Rules

- **HSTS** with `includeSubDomains` and `preload` once verified.
- **CSP**: start strict; loosen only as required by Metronic / specific libraries. Document each `'unsafe-*'` source.
- **X-Frame-Options DENY** unless the app is intentionally embeddable.
- **Permissions-Policy** denies unused features.

---

## 6. TLS

- **TLS 1.2 minimum**, TLS 1.3 preferred.
- **Disable weak ciphers.** Use the Mozilla "Intermediate" config.
- **Auto-renewal** via Let's Encrypt + certbot, or cloud-provider managed certificates.
- **Internal traffic encrypted too** (e.g. service-to-service, app-to-DB) when crossing trust boundaries.

---

## 7. Account Security Defaults

- **Password minimum 12 characters**, mixed character classes.
- **Password history**: don't reuse the last 5 passwords.
- **Lockout**: 5 failed attempts → 15-minute lockout.
- **MFA**: required for admins; optional for others.
- **Session timeout**: 15 min access token; 7-day refresh.
- **Password reset** sends a one-time link via email (single-use, 30-min TTL).
- **Email change** requires re-authentication and confirms via the new address.

---

## 8. Data Protection

### At rest
- **Database backups encrypted** (managed services do this by default).
- **Sensitive columns encrypted** with AES-GCM via app code, where regulation requires it.
- **Encryption keys in a key vault**, rotatable.

### In transit
- **TLS for all external traffic.**
- **mTLS for service-to-service** (when zero-trust architecture).

### In memory
- **Don't log secrets**, ever.
- **Clear sensitive memory** when known sensitive (e.g. password byte arrays). The CLR mostly handles this; explicit clearing is for high-sensitivity contexts.

---

## 9. Backup and Disaster Recovery

- **Automated daily backups** of the database.
- **Offsite storage** (different region than the DB).
- **Test restore quarterly.** A backup that hasn't been restored is not a backup.
- **RPO / RTO documented** per environment.

---

## 10. Incident Response

When a security incident is suspected:

1. **Don't panic. Don't delete logs.** Preserve evidence.
2. **Engage on-call** + security lead.
3. **Document the timeline** in a private channel.
4. **Contain**: revoke tokens, rotate secrets, block IPs.
5. **Eradicate**: patch the vulnerability.
6. **Recover**: verify systems are clean.
7. **Postmortem**: blameless writeup; fix systemic issues.

Runbook: `docs/runbooks/incident-response.md`.

---

## 11. Common Mistakes

| Mistake                                                          | Fix                                                              |
|------------------------------------------------------------------|------------------------------------------------------------------|
| Endpoint authorized but check the wrong permission               | One permission per concern; named clearly                        |
| User-input concatenated into raw SQL                             | Parameters / `FromSqlInterpolated`                               |
| `dangerouslySetInnerHTML` for "rich text"                        | Render through a vetted Markdown lib + DOMPurify                 |
| CORS `*` with credentials                                        | Origin whitelist                                                 |
| Token in URL path / query                                        | Always in `Authorization` header                                 |
| MFA optional for admin                                           | MFA required for admin                                           |
| Reusing JWT signing key across environments                      | Per-env keys                                                     |
| Backups never restored                                           | Quarterly restore test                                           |
| Deploying with stale CSP that's broken                           | Test CSP in staging; check browser console                       |
| Missing rate limit on `/login`, `/forgot-password`               | 5/min per IP at minimum                                          |
| No alerts on auth failure spikes                                 | Wire alerts; brute force is loud if you listen                   |
