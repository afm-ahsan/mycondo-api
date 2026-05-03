# MyCondo Kickoff

> Phase 0 deliverable per `docs/conventions/08-templates/new-project-kickoff.md`. Source of truth for project understanding; updated as understanding evolves. The contractually-binding spec is `docs/Proposal.pdf` (MC-PROP-2026-001, v2.0, signed 2026-03-17).

## Resolved decisions

| Date | Decision | Rationale |
|------|----------|-----------|
| 2026-05-03 | **Frontend stack: Metronic + RTK Query + React Hook Form + Zod + Tailwind v4** (NOT Ant Design + TanStack Query/Router as in proposal §06) | Convention library default wins. Existing `Client/` scaffold is already Metronic-based. Frontend library choice is implementation detail; client deliverables (pages, workflows, NFRs) unchanged. Deviation from proposal §06 to be captured in an ADR before Phase 2 begins. |
| 2026-05-03 | **OpenAPI codegen: `openapi-typescript` + `@rtk-query/codegen-openapi`** | Follows from the RTK Query decision above. |
| 2026-05-03 | **Repo layout: two separate GitHub repos** — `mycondo-api` (https://github.com/afm-ahsan/mycondo-api.git) and `mycondo-web` (https://github.com/afm-ahsan/mycondo-web.git) | Matches proposal §03 ("two clean repos, lowercase, hyphenated, role-based, independent deployment"). Each repo's root IS the project folder; no `MyCondo.Core/` / `MyCondo.Client/` sibling layout. Convention library is duplicated into both repos' `docs/conventions/` (drift risk accepted). |
| 2026-05-03 | **DB schema strategy: schema-per-module (18 schemas)** | Convention default is single `app` schema; overridden by MyCondo.md §06 to surface module ownership at the DB layer and ease future microservice extraction. |
| 2026-05-03 | **All Phase 1 naming locked** — see "Phase 1 — Naming and Structure" section below | Repos, .NET solution + projects, frontend folders, schemas + roles, cache prefix, env-var prefix, URLs, tag format. Don't revisit casually. |
| 2026-05-03 | **Phase 2 + Phase 3 foundation complete** — see commits `77f51ff` / `e32103b` (bootstrap) and the Phase 3 commit | Both repos pushed to GitHub. Backend: 11-project .NET 10 solution, Domain/Application/Infrastructure/Api layers, all 4 MediatR pipeline behaviors, EF Core + interceptors (audit/soft-delete/domain-event), Serilog, OTel, OpenAPI 3.1 + Scalar, JWT setup, health checks, CORS, rate limit, initial migration creating 18 schemas. Frontend: RTK store + RTK Query base + JWT-aware fetch + auto-refresh, Zod env loader, ApiError + toUserMessage, RequireAuth/RequirePermission stubs, ErrorBoundary/LoadingSpinner/EmptyState. Build + 6 tests green. Frontend build green (3.3 MB bundle, code-split deferred). |

## ✅ License posture — OSS only (resolved 2026-05-03)

Two commercial-license libraries were initially pulled in via convention defaults. Both replaced with MIT-licensed alternatives the same day:

| Library replaced | Replacement | Notes |
|------------------|-------------|-------|
| ~~MediatR 14.x~~ (Lucky Penny Software, paid) | **`Mediator` 3.0.2** (martinothamar/Mediator, MIT) | Source-generated, ~ value-task based. API differs slightly: `IPipelineBehavior<TMessage, TResponse>` returns `ValueTask<TResponse>` and takes `MessageHandlerDelegate<TMessage, TResponse> next` (parameter order: message, next, ct). `IPublisher` → `IMediator`. Source generator dislikes open-generic notifications, so domain events bypass Mediator entirely (own `IDomainEventHandler<T>` interface + DI-resolution dispatcher). |
| ~~FluentAssertions 8.x~~ (Xceed, paid) | **`AwesomeAssertions` 9.4** (MIT community fork of FA 7.x) | Drop-in API-compatible; only csproj refs change. |

Phase 3 build now runs clean — no commercial-license warnings.

## 1. One-line description

A multi-tenant SaaS platform that consolidates building automation and property management — service charges, invoicing, payments, complaints, vendors, payroll, security — into a single tenant-isolated system for residential and commercial property managers, starting with ARP Flat Owner's Association.

## 2. Who uses it

- **Primary users**
  - SuperAdmin / Tenant owner — 1 per tenant
  - BuildingAdmin / building manager — 1–3 per building
  - Treasurer / finance committee — 1–3 per tenant
  - Secretary — 1 per building
  - SecurityHead — 1 per building
  - Owner / Tenant (resident) — typical complex 200–2,000 units; up to 500 concurrent SignalR connections / tenant
- **Secondary users**
  - Auditor (read-only)
  - Vendor (Phase 2 self-service)
  - Guard (Phase 2 mobile-style desk usage)
- **Operator**
  - Consultant (Ajwad Technologies) — super-admin-level provisioning + warranty support

## 3. Business goals

- Replace spreadsheets/paper registers for service charges, invoicing, payments, complaints, vendor management with one auditable system.
- Hard tenant isolation — each property association gets its own logical environment, enforced at the DB row level (PG RLS), immune to application bugs.
- Append-only financial integrity — double-entry ledger, FIFO allocation under row locking, idempotent mutations on financial endpoints. No deletes, only reversing entries.
- Production-ready in 6 months for ARP — 11 MVP modules in 16 weeks, 3 Phase 2 modules in following 8 weeks.
- Architecture preserves a clean migration path to microservices (schema-per-module, MediatR-only cross-module communication) without paying the microservice tax up front.

## 4. Modules in scope

MVP — 11 modules, ~92 endpoints, target Weeks 1–16.

| # | Module | Aggregate(s) | APIs | Notes |
|---|--------|--------------|------|-------|
| 1 | Tenant Provisioning | `Tenant` | 4 | Subdomain routing, default seed data, lifecycle (create/activate/suspend) |
| 2 | Auth & Authorization | `User`, `RefreshToken`, `MfaEnrollment` | 6 | ASP.NET Identity 10 + Argon2id + JWT (15m access / 7d rotating refresh); MFA infra (TOTP), UI Phase 2 |
| 3 | Property Hierarchy | `Building`, `Tower`, `Floor`, `Unit` | 12 | Multi-tower; unit types; occupancy tracking; CSV bulk import |
| 4 | Resident Management | `Resident`, `Ownership`, `Lease` | 8 | Fractional ownership (must sum to 100%); lease overlap detection |
| 5 | Service Charges | `ServiceChargeRule` | 4 | Per-building rules: fixed / sqft / unit; frequency; unit-type filter |
| 6 | Invoice & Billing | `Invoice`, `InvoiceLine` | 8 | Idempotent batch generation; sequential numbering; void → reversing entry |
| 7 | Payment & Collection | `Payment`, `PaymentAllocation`, `LedgerEntry` | 4 | Idempotency-key required; FIFO allocation w/ `SELECT … FOR UPDATE`; overpayment logged |
| 8 | Vendor & Expense Mgmt | `Vendor`, `VendorContract`, `VendorBill`, `Expense` | 10 | Gas/electricity/water/gardening/parking; bill capture; approval workflow |
| 9 | Complaints & Work Orders | `Ticket`, `Comment`, `WorkOrder` | 14 | SLA tracking; auto-assign by category; status workflow; comment threads |
| 10 | Roles & Permissions | `Role`, `Permission`, `RoleAssignment` | 10 | 40+ permissions; matrix UI; clone/compare; building-scoped assignments |
| 11 | Notifications & Reports | `Notification`, `NotificationTemplate`, materialized views | 12 | Multi-channel (in-app/email/SMS); financial KPIs, aging, collection report |

Phase 2 — 3 modules, target Weeks 17–24.

| # | Module | Aggregate(s) | Notes |
|---|--------|--------------|-------|
| 12 | Facility Booking | `Facility`, `Booking`, `BlackoutDate` | Calendar booking with pessimistic locking; hourly availability; approval; deposits |
| 13 | Preventive Maintenance | `MaintenanceSchedule`, `WorkOrder` (reused), `Checklist` | Schedule templates daily→annual + custom; auto WO via daily 04:00 BST job |
| 14 | Visitor Management | `Visitor`, `VisitorPass`, `GateLog` | QR + OTP passes; check-in/out; real-time host alert (SignalR); recurring; auto-expiry hourly |

Cross-cutting MVP deliverables (not numbered modules but in scope):
- Document management (upload validation, SHA-256, versioning, signed URLs, entity linking)
- Audit trail (auto before/after per mutation, correlation ID, IP/UA, partitioned monthly)
- Background jobs (monthly billing, daily late fees, notification dispatch, expense reminders)
- Redis cache (tenant-prefixed keys, TTL strategy, event-driven invalidation)
- React SPA (login, sidebar, all CRUD pages, dashboard charts, notification bell)
- DevOps (Docker Compose, Dockerfile, GitHub Actions CI/CD, OpenAPI 3.1 + Scalar)
- Observability (structured logging, health checks, /metrics, correlation propagation)

## 5. Modules explicitly OUT of scope

Per proposal §17 Exclusions:

- **E1** Native mobile app (iOS/Android) — Phase 3 roadmap
- **E2** Online payment gateway integration — Phase 3
- **E3** IoT / smart-building sensor integration — Phase 4
- **E4** White-label branding per tenant — Phase 3 (architecture supports it)
- **E5** Data migration from existing systems (Excel/paper/legacy software) — quoted separately
- **E6** Multi-language / RTL support (Bangla, etc.) — Phase 3 (i18n-ready)
- **E7** Formal compliance certifications (SOC 2, ISO 27001) — architecture aligns, audit not in scope
- **E8** 24/7 ops support — best-effort during 60-day warranty only; retainer available
- **E9** Cloud infrastructure management beyond delivery — client owns AWS account

## 6. User roles and permissions (sketch)

40+ permissions named `<module>.<resource>.<action>` (e.g. `billing.invoice.create`). 10 default roles seeded per tenant.

| Role | Typical user | Permission set (sketch) |
|------|--------------|-------------------------|
| SuperAdmin | Tenant owner | `*` (all) |
| BuildingAdmin | Building manager | `billing.*`, `payments.*`, `complaints.*`, `vendors.*`, `payroll.*`, `property.*`, `residents.*` (building-scoped) |
| Treasurer | Finance committee | `billing.*`, `payments.*`, `expenses.*`, `reports.financial.*` |
| Secretary | Building secretary | `complaints.*`, `notifications.*`, `documents.*`, `residents.read` |
| SecurityHead | Security supervisor | `security.*`, `complaints.ticket.read` |
| Owner | Flat owner | `*.read` scoped to own data + own building (RLS + handler check) |
| Tenant | Renter | Subset of Owner — no ownership-related reads |
| Vendor (P2) | External vendor | `vendors.contract.read`, `vendors.invoice.create` |
| Guard (P2) | Security guard | `security.visitor.create`, `security.visitor.update` |
| Auditor | Read-only oversight | `*.read` |

Notes:
- Permissions are **never** in the JWT — only roles + tenant + building scope. Resolved server-side, cached in Redis (`perms:{user_id}:{tenant_id}`, 15-min TTL).
- Building-scoped permissions are validated at handler level against route-bound `buildingId`; client-supplied `buildingId` is never trusted.
- Tenants can clone defaults and create custom roles via the Roles UI (Module 10).

## 7. Critical workflows (happy paths)

1. **Monthly billing run.** BuildingAdmin clicks "Generate invoices" for May 2026 → idempotent batch creates 1 invoice per active unit per applicable charge rule → invoices get sequential numbers → Notifications module dispatches in-app + email per resident preference. Re-running with same idempotency key returns the same batch (no duplicates). Target ≤ 30 s for 1,000 units.
2. **Payment allocation.** Resident pays BDT 5,000 → BuildingAdmin records payment with `X-Idempotency-Key` → FIFO allocator (`SELECT … FOR UPDATE`) applies it across oldest outstanding invoices first → ledger entries written → invoice status auto-flips to PAID/PARTIAL → resident sees updated balance in real time via SignalR.
3. **Complaint lifecycle.** Resident files a complaint via the portal → ticket created with category and SLA clock → auto-assigned to appropriate staff → comment threading + status transitions → optional work-order linkage → resolved/closed → resident notified at each step (in-app, email).
4. **New tenant onboarding** (10-day path per §12). Super-admin provisions tenant → admin defines Project→Building→Tower→Floor→Unit hierarchy (CSV bulk import) → CSV imports residents/owners with fractional-ownership validation → service charge rules + late fee policy + dry-run invoice batch → vendors + payroll setup → roles assigned, login credentials sent → first real billing cycle runs under consultant supervision.
5. **Visitor pre-registration** (Phase 2). Resident pre-registers visitor → system generates QR + OTP → guard scans QR at gate, validates OTP → check-in event → SignalR pushes "Visitor X has arrived" to host within 1 s → check-out captured at exit → pass auto-expires 2 h past expected departure.

## 8. Non-functional constraints

| Category | Target |
|---|---|
| API p95 latency | ≤ 300 ms (k6, 1,000 units, 100 concurrent users) |
| API p99 latency | ≤ 800 ms |
| Dashboard initial load | ≤ 2.0 s (Lighthouse CI on staging) |
| Invoice batch (1,000 units) | ≤ 30 s |
| Real-time notification latency | ≤ 1.0 s end-to-end via SignalR |
| Concurrent users | ≥ 200 / tenant |
| Production uptime SLA | 99.5% (≈3.6 h downtime/month, excl. announced maintenance) |
| RTO | 4 hours |
| RPO | 15 minutes |
| Maintenance windows | Sat 02:00–04:00 BST, ≥48 h pre-announced |
| Tenants per DB | up to 100 (RLS) |
| Units per tenant | up to 2,000 |
| SignalR connections | up to 500 / tenant |
| Background jobs | ≥ 1,000 jobs/min |
| Doc storage / tenant | up to 50 GB (configurable) |
| Browsers | Chrome, Edge, Safari, Firefox — latest 2 versions |
| Mobile responsiveness | usable down to 360 px |
| Accessibility | WCAG 2.1 AA target on resident-facing pages |
| i18n | English MVP; Bangla pluggable Phase 3 |
| Time zone | UTC stored; rendered per-tenant (default Asia/Dhaka) |
| Currency | BDT primary; ISO 4217 ready for multi-currency later |
| Compliance posture | OWASP Top 10 engineered for; GDPR principles + Bangladesh DPA aligned; SOC 2 readiness foundations only |

## 9. Integrations

| System | Purpose | Direction | Phase | Owner |
|---|---|---|---|---|
| SendGrid (or equivalent) | Transactional email (invoices, password reset, welcome, complaint updates) | Outbound | MVP | Client (A4) |
| Twilio (or equivalent) | SMS notifications | Outbound | MVP | Client (A4) |
| AWS S3 / MinIO (dev) | Tenant-partitioned object storage, signed URLs, SSE | Bidirectional | MVP | Consultant scaffolds, client owns prod |
| AWS RDS (PostgreSQL 18) | Primary DB, Multi-AZ, encrypted at rest | Bidirectional | MVP | Client AWS |
| AWS ECS Fargate | API + jobs container hosting | — | MVP | Client AWS |
| AWS Secrets Manager / Parameter Store | Secrets, JWT keys, third-party API tokens | Inbound | MVP | Client AWS |
| GitHub Actions | CI/CD (build, test, security scan, deploy) | — | MVP | Consultant |
| Trivy + Dependabot | Container + dependency scanning | — | MVP | Consultant |
| Seq (dev) / CloudWatch (prod) | Structured log aggregation | Outbound | MVP | Mixed |
| OpenTelemetry collector → CloudWatch / Grafana | Metrics + traces | Outbound | MVP | Consultant scaffolds |
| Hangfire dashboard | Job monitoring (operator-only) | UI | MVP | Consultant |
| Payment gateway (bKash / Nagad / SSLCommerz) | Online payment | — | **Phase 3 (E2 — out of scope)** | — |

## 10. Deadlines / milestones

Fixed 24-week timeline, milestone-gated payments. Total: BDT 2,50,000.

| ID | Title | Week | Acceptance criteria | Payment |
|---|---|---|---|---|
| M1 | Foundation | 4 | Tenant provisioning, auth flow, permissions enforced, OpenAPI 3.1 spec live with Scalar UI, CI/CD green | BDT 50,000 (20%) |
| M2 | Property + Residents | 8 | Full property CRUD; resident + ownership + lease working; React live | BDT 37,500 (15%) |
| M3 | Financial Engine + Vendor | 13 | Billing generates invoices; payments allocate (FIFO) correctly; aging accurate; vendor/expense functional | BDT 62,500 (25%) |
| M4 | Operations + MVP complete | 16 | Complaints end-to-end; notifications deliver; documents upload; observability live | BDT 50,000 (20%) |
| M5 | Phase 2 + Production launch | 24 | All 14 modules working; load tested; UAT passed; runbook delivered; production deployed | BDT 50,000 (20%) |

Internal phase boundaries (proposal §13):
- **Phase 1 — Foundation & Core Infra** — Weeks 1–4 (4 wk) → M1
- **Phase 2 — Property & Resident Mgmt** — Weeks 5–8 (4 wk) → M2
- **Phase 3 — Financial Engine & Vendor** — Weeks 9–13 (5 wk; longest, highest risk) → M3
- **Phase 4 — Ops, Notifications, Polish** — Weeks 14–16 (3 wk) → M4
- **Phase 5 — Phase 2 modules + Launch** — Weeks 17–24 (8 wk) → M5

Post-launch:
- 60-day warranty (M5 + 60 days): bug fixes, perf tuning for real-load issues, emergency security patches
- Final knowledge-transfer session at warranty end
- Optional retainer (BDT 15,000–25,000/month) post-warranty

## 11. Open questions for the client / for Ahsan

Questions to resolve before / during Phase 1.

**For ARP (client):**
- [ ] **A2 — Sample data delivery date.** Proposal commits client to deliver sample buildings, charge types, residents, vendors by Week 2. Confirm date and format (CSV templates we'll publish).
- [ ] **A3 — AWS account.** When will the AWS Organizations account be ready? Need it provisioned by start of M3 (Week ~9) for staging deployment of the financial engine.
- [ ] **A4 — Email + SMS providers.** SendGrid + Twilio (or equivalents) — confirm choice; need API keys provisioned by Week 6 (before M3 notifications work).
- [ ] **A7 — Single point of contact.** Proposal §23 has the client primary POC `[ to be filled ]`. Need name, phone, email.
- [ ] **First production tenant scope.** Is "ARP Flat Owner's Association" itself the first production tenant onboarded during M5? How many buildings / towers / units? Single complex or multi-site?
- [ ] **Service charge rules at ARP.** Fixed-amount, per-sqft, or per-unit-type? Monthly or quarterly? Late fee policy (% / flat / grace days)?
- [ ] **Multi-tower at ARP.** Does ARP have multiple towers / blocks, or a single building? Determines property-hierarchy demos and seed depth.
- [ ] **Payment recording mode.** With gateway integration excluded (E2), MVP records payments via manual entry only — cash, cheque, bKash/Nagad reference number, bank transfer reference. Confirm no need for any minimal "mark as paid via mobile money" workflow at launch.
- [ ] **Currency display preference.** "BDT 1,250" vs "৳1,250" vs "Tk. 1,250"? Pick one for the SPA.
- [ ] **Tenant subdomain for ARP.** Reserve `arp.mycondo.app` (or alternative)? Confirm now to avoid clashes.
- [ ] **WCAG 2.1 AA scope.** Resident-facing pages only (per §07), correct? Admin pages best-effort?
- [ ] **Data residency.** Proposal §08 says primary region Singapore (ap-southeast-1), configurable per tenant. ARP confirms Singapore is acceptable, or insists on a Bangladesh-region option (which AWS doesn't currently offer)?

**For Ahsan (architect-side decisions):**
- [x] ~~**Frontend stack reconciliation — BLOCKER for Phase 2.**~~ **Resolved 2026-05-03**: Metronic + RTK Query + React Hook Form + Zod + Tailwind v4 (convention library wins; proposal §06 set aside). ADR to be written before Phase 2 begins documenting the deviation from proposal §06.
- [ ] **Hangfire vs Quartz.NET vs custom.** Proposal commits to Hangfire + PostgreSQL storage. Confirm. (Recommended — battle-tested, dashboard included, matches budget.)
- [ ] **Argon2id parameters.** Pin `m=`, `t=`, `p=` cost factors before identity module work begins — needs to be tuned to deployment hardware (Fargate vCPU/RAM tier) and recorded in an ADR.
- [ ] **Invoice number format.** Proposal says "sequential numbering" — per-tenant per-building per-year? `INV-{building}-{yyyy}-{seq}`? Pick a format up front; changing later corrupts user mental model.
- [ ] **Document storage cap enforcement.** 50 GB / tenant configurable cap — enforce at upload time (hard) or alert + soft-throttle? Pick before docs module ships.
- [x] ~~**OpenAPI codegen target.**~~ **Resolved 2026-05-03** (follows from frontend stack): `openapi-typescript` + `@rtk-query/codegen-openapi`.
- [ ] **Architecture testing harness.** NetArchTest is named in the proposal; confirm — and set up the rule list (no cross-module direct refs, every endpoint has `[RequirePermission]` or `[AllowAnonymous]`, etc.) at M1.

## 12. First vertical slice

**Pick:** `Property Hierarchy` module — narrowed to `Building` aggregate only. Specifically: `Building.Create`, `Building.GetById`, `Building.List`.

**Why this and not something else:**
- It's master data — nothing depends on it being complete (Towers/Floors/Units come right after, but the slice doesn't need them).
- It's the natural anchor of the data model — every other module points at a `BuildingId`, so getting the tenant + building scoping right here pays off across all 14 modules.
- It exercises every layer of the architecture in one cycle: tenant context middleware → RLS-enabled DB context → EF Core config (snake_case, audit, soft-delete, domain-event interceptors) → MediatR command/query + FluentValidation → REST endpoint with `[RequirePermission("property.building.create")]` → OpenAPI codegen → frontend list page + create form + detail page → MultiTenancyTests + integration tests + E2E happy path.
- Vendors are slightly simpler in isolation but don't surface multi-tenant building scope, which is the highest-risk plumbing in the system.

**Out of the slice (defer to M2):** Tower/Floor/Unit, CSV import, occupancy tracking, edit/delete UX. Just enough to prove the architecture holds.

**Slice complete when:** A test admin can log in, create a building, see it in the list, fetch by id, and a second tenant's admin (in a parallel test) cannot see it. All tests in the build pipeline (`UnitTests`, `IntegrationTests`, `MultiTenancyTests`, `ArchitectureTests`, `E2E`) are green. Convention compliance is reviewed against `docs/conventions/` before going broad.

---

## Phase 1 — Naming and Structure (resolved 2026-05-03)

These decisions are locked. Changing any of them later requires deliberate rename work across configs, scripts, secrets, container images, and ADRs — don't revisit casually.

### Repository

| Decision | Value | Notes |
|---|---|---|
| Repo layout | **Two separate GitHub repos** — `mycondo-api` and `mycondo-web` | Per proposal §03. Each repo's root IS the project folder; no `MyCondo.Core/` / `MyCondo.Client/` parent layout. |
| `mycondo-api` remote | `https://github.com/afm-ahsan/mycondo-api.git` | Backend repo |
| `mycondo-web` remote | `https://github.com/afm-ahsan/mycondo-web.git` | Frontend repo |
| Local paths | `D:\Workspace\Projects\mycondo-api\`, `D:\Workspace\Projects\mycondo-web\` | |
| Conventions library | Duplicated into both repos at `docs/conventions/` (copied from `D:\Workspace\Templates\Template2\docs\conventions\`) | Drift risk accepted; conventions library is fairly stable. Refresh from template on demand. |
| Default branch | `main` | |
| Branch naming | `feat/<desc>`, `fix/<desc>`, `chore/<desc>`, `docs/<desc>`, `refactor/<desc>`, `test/<desc>`, `perf/<desc>`, `hotfix/<desc>` | Per MyCondo.md §10 / conventions |
| Commit format | Conventional Commits (`<type>(<scope>): <subject>`); enforced by `commitlint` in CI | |
| Tag format | `v<major>.<minor>.<patch>` (e.g. `v1.0.0`) — independent semver per repo since they release separately | |

### Backend

| Decision | Value | Notes |
|---|---|---|
| Backend repo root | `mycondo-api/` | Repo root is the project folder. No parent `MyCondo.Core/` wrapper. |
| Solution file | `mycondo-api/MyCondo.sln` | |
| .NET root namespace | `MyCondo` | |
| Layer projects | `MyCondo.Domain`, `MyCondo.Application`, `MyCondo.Infrastructure`, `MyCondo.Api`, `MyCondo.Shared` | Under `mycondo-api/src/` |
| Test projects | `MyCondo.Domain.UnitTests`, `MyCondo.Application.UnitTests`, `MyCondo.Infrastructure.IntegrationTests`, `MyCondo.Api.IntegrationTests`, `MyCondo.MultiTenancyTests`, `MyCondo.ArchitectureTests` | Under `mycondo-api/tests/` |
| Module project naming | `MyCondo.Modules.<Module>` (e.g. `MyCondo.Modules.Billing`) | |
| Internal namespacing | `MyCondo.<Layer>.*` or `MyCondo.Modules.<Module>.<Layer>.*` (e.g. `MyCondo.Modules.Billing.Application.Commands.GenerateInvoicesCommand`) | |
| .NET SDK pin | `global.json` pinning .NET 10 LTS | |
| Package management | Central via `Directory.Packages.props` | |
| Migration runner | `mycondo-api/tools/MyCondo.DbMigrator/` standalone runner | |
| Container image (api) | `ghcr.io/mycondo/api:<semver>` | |
| Container image (jobs) | `ghcr.io/mycondo/jobs:<semver>` | |
| docker-compose.yml | Lives in `mycondo-api/` (it's the local-infra owner — postgres, redis, mailhog, etc.) | Frontend dev points at it via `VITE_MYCONDO_API_BASE_URL=http://localhost:5000` |

### Frontend

| Decision | Value | Notes |
|---|---|---|
| Frontend repo root | `mycondo-web/` | Repo root is the project folder. |
| npm package name | `@mycondo/web` | private package (not published to public npm) |
| Stack | Metronic React Vite + React 18.3 + TypeScript 5.6+ strict + RTK Query + React Hook Form + Zod + Tailwind v4 + Vitest + Playwright | Per Resolved Decisions §1, deviates from proposal §06 |
| Module folder pattern | `src/modules/<feature>/` mirrors backend module names 1:1 (lowercase, hyphenated for multi-word) | |
| Container image (web) | `ghcr.io/mycondo/web:<semver>` | nginx-served in prod |

### Database

| Decision | Value | Notes |
|---|---|---|
| DB engine | PostgreSQL 18 | Per proposal §06 |
| DB name | `mycondo` | per env: `mycondo_dev`, `mycondo_staging`, `mycondo` (prod) |
| Roles | `mycondo_app` (runtime), `mycondo_migrator` (DDL), `mycondo_readonly` (BI/reporting) | |
| Schema strategy | **Schema-per-module** (18 schemas, see table below) | Convention default is single `app` schema; overridden by MyCondo.md §06 to make module ownership visible at DB level and ease future microservice extraction. |
| Naming | `snake_case` everywhere; tables plural; PKs `id` (uuid v7); FKs `<singular>_id`; indexes `ix_<table>_<cols>`; uniques `ux_…`; FK constraints `fk_<table>_<ref>`; checks `ck_<table>_<rule>`; RLS policies `rls_<table>_tenant_isolation`; views `vw_…`; mat-views `mv_…`; functions `fn_…`; triggers `tr_<table>_<event>` | Per MyCondo.md §06 |
| Multi-tenancy | RLS `ENABLED + FORCED` on every tenant-scoped table; `tenant_id` always leads composite indexes; tenant context set per-connection from JWT via `SET app.current_tenant_id` | |
| Audit | `audit.changes` partitioned monthly, append-only, populated via EF Core SaveChanges interceptor | |
| Financial integrity | Append-only `payments.ledger_entries`; voids = reversing entries; FIFO allocation under `SELECT … FOR UPDATE`; idempotency keys in `payments.idempotency_keys` | |

#### Schema → module map

| # | Schema | Owner module | Primary aggregate(s) |
|---|--------|--------------|----------------------|
| 1 | `tenancy` | Tenant Provisioning | `Tenant` |
| 2 | `identity` | Auth & Authorization + Roles & Permissions | `User`, `Role`, `Permission`, `RoleAssignment`, `RefreshToken`, `MfaEnrollment`, `MfaRecoveryCode` |
| 3 | `property` | Property Hierarchy | `Building` (root) — owns `Tower`, `Floor`, `Unit` as descendants |
| 4 | `residents` | Resident Management | `Resident`, `Ownership` |
| 5 | `leasing` | Resident Management (lease side) | `Lease` |
| 6 | `billing` | Service Charges + Invoice & Billing | `ServiceChargeRule`, `Invoice` (root, owns `InvoiceLine`) |
| 7 | `payments` | Payment & Collection | `Payment` (owns `PaymentAllocation`), `LedgerEntry`, `IdempotencyKey` |
| 8 | `expenses` | Vendor & Expense Mgmt (expense side) | `VendorBill`, `Expense` |
| 9 | `vendors` | Vendor & Expense Mgmt (vendor side) | `Vendor`, `VendorContract` |
| 10 | `payroll` | Payroll | `StaffMember`, `SalaryDisbursement`, `AttendanceRecord` |
| 11 | `complaints` | Complaints & Work Orders (ticket side) | `Ticket` (owns `Comment`) |
| 12 | `maintenance` | Complaints & Work Orders (work-order side) + Preventive Maintenance (P2) | `WorkOrder`, `MaintenanceSchedule` (P2), `Checklist` (P2) |
| 13 | `amenities` | Facility Booking (P2) | `Facility`, `Booking`, `BlackoutDate` |
| 14 | `security` | Visitor Management (P2) | `Visitor`, `VisitorPass`, `GateLog` |
| 15 | `notifications` | Notifications | `Notification`, `NotificationTemplate`, `NotificationDispatchLog` |
| 16 | `documents` | (Cross-cutting) | `Document`, `DocumentVersion` |
| 17 | `reporting` | Reports (Notifications & Reports module) | `mv_collection_summary`, `mv_invoice_aging`, etc. — materialized views, no aggregates |
| 18 | `audit` | (Cross-cutting) | `AuditChange` (partitioned monthly, append-only) |

### Cache, env, URL

| Decision | Value | Notes |
|---|---|---|
| Cache key prefix | `mycondo:` — full pattern `mycondo:<env>:<tenant_slug>:<purpose>:<key>` | E.g. `mycondo:prod:arp:perms:01HX...:01HX...`. Verbose but readable; Redis cluster slot count is fine. Conventions suggest 3-letter code; we deviate for legibility. |
| Permission cache key | `mycondo:perms:{user_id}:{tenant_id}` (15-min TTL) | Per MyCondo.md §08 |
| Env var prefix | `MYCONDO_` (e.g. `MYCONDO_DB_CONNECTION_STRING`, `MYCONDO_REDIS_CONNECTION_STRING`, `MYCONDO_JWT_SIGNING_KEY`, `MYCONDO_S3_BUCKET`, `MYCONDO_SENDGRID_API_KEY`) | Per MyCondo.md §09 |
| Frontend env prefix | `VITE_MYCONDO_` (e.g. `VITE_MYCONDO_API_BASE_URL`, `VITE_MYCONDO_APP_NAME`, `VITE_MYCONDO_ENV`) | Vite requires `VITE_` prefix to expose to client |
| Production URLs | `api.mycondo.app`, `app.mycondo.app`, `docs.mycondo.app`, `status.mycondo.app` | Per MyCondo.md §09 |
| Staging URLs | `api.staging.mycondo.app`, `app.staging.mycondo.app` | |
| Tenant URL pattern | `<tenant-slug>.mycondo.app` — first tenant: `arp.mycondo.app` | Reserve `arp` slug at provisioning time |

### Module → frontend folder map

Backend module name → frontend folder (one-to-one, kebab-case for multi-word).

| Backend module | Frontend folder |
|----------------|-----------------|
| `MyCondo.Modules.Tenancy` | `src/modules/tenancy/` |
| `MyCondo.Modules.Identity` | `src/modules/identity/` |
| `MyCondo.Modules.Property` | `src/modules/property/` |
| `MyCondo.Modules.Residents` | `src/modules/residents/` |
| `MyCondo.Modules.Leasing` | `src/modules/leasing/` |
| `MyCondo.Modules.Billing` | `src/modules/billing/` |
| `MyCondo.Modules.Payments` | `src/modules/payments/` |
| `MyCondo.Modules.Expenses` | `src/modules/expenses/` |
| `MyCondo.Modules.Vendors` | `src/modules/vendors/` |
| `MyCondo.Modules.Payroll` | `src/modules/payroll/` |
| `MyCondo.Modules.Complaints` | `src/modules/complaints/` |
| `MyCondo.Modules.Notifications` | `src/modules/notifications/` |
| `MyCondo.Modules.Documents` | `src/modules/documents/` |
| `MyCondo.Modules.Reporting` | `src/modules/reporting/` |
| `MyCondo.Modules.Amenities` (P2) | `src/modules/amenities/` |
| `MyCondo.Modules.Maintenance` (P2) | `src/modules/maintenance/` |
| `MyCondo.Modules.Security` (P2) | `src/modules/security/` |

---

*Last updated: 2026-05-03. Update this file when answers come back from ARP, when decisions are made, or when scope changes via CR.*
