# Naming Conventions (Cross-Cutting)

A consolidated reference for all naming rules. Each layer's deeper file has the full context; this is the cheat-sheet.

---

## 1. C# / .NET

| Kind                            | Convention                  | Example                              |
|---------------------------------|-----------------------------|--------------------------------------|
| Namespace                       | `PascalCase`                | `<Project>.Application.Customers`    |
| Class / record / struct         | `PascalCase`                | `CreateCustomerCommand`              |
| Interface                       | `IPascalCase`               | `ICustomerRepository`                |
| Method                          | `PascalCase`, verb-first    | `CreateAsync`, `CalculateTotal`      |
| Property                        | `PascalCase`                | `CreatedAtUtc`, `IsActive`           |
| Field — private                 | `_camelCase`                | `_customers`                         |
| Field — `const` / `static readonly` | `PascalCase`            | `MaxRetryCount`                      |
| Local / parameter               | `camelCase`                 | `customerId`, `command`              |
| Async method                    | suffix `Async`              | `GetByIdAsync`                       |
| Generic param                   | `T`, `TKey`, `TValue`       | `Result<TValue, TError>`             |
| Enum value                      | `PascalCase`                | `OrderStatus.Submitted`              |

### Special suffixes

| Suffix              | Use case                         |
|---------------------|----------------------------------|
| `Command`           | CQRS write request               |
| `Query`             | CQRS read request                |
| `CommandHandler`    | Command handler                  |
| `QueryHandler`      | Query handler                    |
| `Validator`         | FluentValidation class           |
| `Dto`               | Data transfer object             |
| `Request` / `Response` | HTTP boundary types           |
| `Configuration`     | EF Core entity configuration     |
| `Behavior`          | MediatR pipeline behavior        |
| `Service`           | Use only when truly stateful; otherwise prefer specific names |
| `Repository`        | Aggregate-level persistence abstraction |

### Forbidden

- Hungarian: `strName`, `bIsActive`.
- Generic suffixes without intent: `Manager`, `Helper`, `Utility`.
- Abbreviations except universally known: `Id`, `Url`, `Http`, `Json`, `Xml`.

---

## 2. TypeScript / React

| Kind                | Convention                | Example                              |
|---------------------|---------------------------|--------------------------------------|
| Component file      | `PascalCase.tsx`          | `CustomerForm.tsx`                   |
| Hook file           | `camelCase.ts`            | `useCustomerActions.ts`              |
| Util / lib file     | `camelCase.ts`            | `formatCurrency.ts`                  |
| Type-only file      | `*.types.ts`              | `customer.types.ts`                  |
| Zod schema          | `*.schema.ts`             | `customer.schema.ts`                 |
| RTK Query slice     | `*.api.ts`                | `customers.api.ts`                   |
| Redux slice         | `*.slice.ts`              | `auth.slice.ts`                      |
| Constant file       | `*.constants.ts`          | `routes.constants.ts`                |
| Folder              | `kebab-case`              | `user-profile/`, `sales-orders/`     |
| Component           | `PascalCase`              | `CustomerForm`                       |
| Hook                | `useCamelCase`            | `useCustomerActions`                 |
| Function            | `camelCase`, verb-first   | `formatCurrency`                     |
| Boolean             | `is/has/should/can`       | `isLoading`, `hasError`              |
| Event handler prop  | `on<Event>`               | `onSelect`                           |
| Event handler impl  | `handle<Event>`           | `handleSelect`                       |
| Constant            | `SCREAMING_SNAKE_CASE`    | `DEFAULT_PAGE_SIZE`                  |
| Type / Interface    | `PascalCase`, no `I` prefix | `Customer`, `CustomerFormProps`    |
| Generic param       | Single capital            | `T`, `TError`                        |

---

## 3. URL / API

| Kind                    | Convention                     | Example                              |
|-------------------------|--------------------------------|--------------------------------------|
| Resource collection     | plural noun, lowercase, kebab  | `/api/customers`                     |
| Resource item           | `/<plural>/{id}`               | `/api/customers/{id}`                |
| Sub-resource            | `/<parent>/{id}/<child-plural>`| `/api/customers/{id}/contact-persons`|
| Action endpoint         | `/<plural>/{id}/<verb>`        | `/api/customers/{id}/deactivate`     |
| Query parameter         | `camelCase`                    | `?pageSize=20&sortBy=name`           |
| Header                  | `Pascal-Kebab-Case`            | `X-Correlation-Id`, `Idempotency-Key`|
| JSON property           | `camelCase`                    | `createdAtUtc`, `isActive`           |

---

## 4. PostgreSQL

| Kind                   | Convention                       | Example                                  |
|------------------------|----------------------------------|------------------------------------------|
| Schema                 | `snake_case`                     | `app`, `auth`, `audit`                   |
| Table                  | singular `snake_case`            | `customer`, `invoice`                    |
| Column                 | `snake_case`                     | `created_at_utc`, `is_active`            |
| Primary key column     | `id`                             | `id`                                     |
| Foreign key column     | `<ref_table>_id`                 | `customer_id`                            |
| Boolean column         | `is_*` / `has_*` / `can_*`       | `is_active`                              |
| Timestamp column (UTC) | `*_at_utc`                       | `created_at_utc`                         |
| Date column            | `*_on`                           | `birth_date_on`                          |
| Money column           | name + `numeric(19, 4)` type     | `total_amount`                           |
| JSON column            | `*_json`                         | `metadata_json`                          |
| Primary key constraint | `pk_<table>`                     | `pk_customer`                            |
| Foreign key constraint | `fk_<table>_<ref_table>`         | `fk_invoice_customer`                    |
| Unique constraint      | `uq_<table>_<columns>`           | `uq_user_email`                          |
| Check constraint       | `ck_<table>_<rule>`              | `ck_invoice_amount_positive`             |
| Exclusion constraint   | `ex_<table>_<rule>`              | `ex_amc_no_overlap`                      |
| Index (b-tree)         | `ix_<table>_<columns>`           | `ix_invoice_customer_status_date`        |
| Unique index           | `uix_<table>_<columns>`          | `uix_customer_email`                     |
| Partial index          | `ix_<table>_<columns>__<predicate>` | `ix_invoice_customer__unpaid`        |
| GIN index              | `gin_<table>_<column>`           | `gin_customer_name_trgm`                 |
| View                   | `v_<purpose>`                    | `v_active_invoice`                       |
| Materialized view      | `mv_<purpose>`                   | `mv_account_balance`                     |
| Function               | `fn_<verb>_<noun>`               | `fn_calculate_invoice_total`             |
| Trigger                | `tg_<table>_<event>_<action>`    | `tg_invoice_before_update_audit`         |

### Forbidden

- Plural tables (`customers`).
- Quoted identifiers (`"Customer"`).
- Reserved words as identifiers (`user`, `order`, `group`).
- Hungarian prefixes (`tbl_`, `t_`, `str_`).

---

## 5. Cache and Lock Keys

```
<service>:<scope>:<id>:<purpose>
```

| Example                          | Meaning                                       |
|----------------------------------|-----------------------------------------------|
| `srm:customer:abc123:detail`     | Cached customer detail DTO                    |
| `srm:invoice:def456:lock`        | Distributed lock for invoice processing       |
| `srm:idem:user-1:/api/orders:ABC`| Idempotency key for a specific request        |
| `srm:outbox:processor:lock`      | Singleton lock for the outbox processor       |

### Rules

- **First segment is the service name** (3-letter abbreviation typically).
- **Constants in `Application/Common/Constants/CacheKeys.cs`** — no scattered string concatenation.
- **TTLs are explicit per key.** No "default cache forever".

---

## 6. Idempotency Keys

```
01923f5c-4b7a-7b8e-9f3d-1a2b3c4d5e6f
```

UUIDv7 generated client-side per mutation. Sent in the `Idempotency-Key` header.

---

## 7. Correlation IDs

```
01923f5c-4b7a-7b8e-9f3d-1a2b3c4d5e6f
```

UUIDv7. Generated by the frontend or by the API gateway. Carried in `X-Correlation-Id` header end-to-end.

---

## 8. Permissions

```
<resource>.<action>
```

Examples:
- `customers.view`
- `customers.manage`
- `quotations.approve`
- `reports.export`
- `system.admin`

### Rules

- **Lowercase, dot-separated.**
- **Resource is plural.**
- **Action is a verb.**
- **`system.admin`** is the global override.

---

## 9. Feature Flags

```
<area>_<descriptor>
```

Examples:
- `reports_enabled`
- `new_billing_flow`
- `experimental_search`

### Rules

- **Snake_case, lowercase.**
- **Boolean unless rollout requires percentages.**
- **Each flag has an expiration date** documented in code or in an issue.

---

## 10. Environment Variables

| Backend (`__` for nesting)             | Frontend (`VITE_` prefix, `_` for nesting) |
|----------------------------------------|---------------------------------------------|
| `ConnectionStrings__Default`           | `VITE_API_BASE_URL`                         |
| `Jwt__SigningKey`                      | `VITE_APP_NAME`                             |
| `Cors__AllowedOrigins__0`              | `VITE_ENV`                                  |
| `Features__ReportsEnabled`             | `VITE_FEATURE_REPORTS`                      |

### Rules

- **Backend: `__` (double underscore)** maps to `:` in IConfiguration.
- **Frontend: `VITE_*` only** is exposed to the browser bundle.

---

## 11. File and Folder Naming Cheat-Sheet

| Layer                 | Folders          | Files                                          |
|-----------------------|------------------|------------------------------------------------|
| .NET solution         | `PascalCase`     | `<Project>.Domain.csproj`                      |
| .NET feature folder   | `PascalCase` plural | `Customers/`, `Quotations/`                 |
| .NET feature file     | `PascalCase`     | `Customer.cs`, `CreateCustomerCommand.cs`      |
| Frontend folder       | `kebab-case`     | `customers/`, `sales-orders/`                  |
| Frontend component    | `PascalCase.tsx` | `CustomerForm.tsx`                             |
| Frontend hook         | `camelCase.ts`   | `useCustomerActions.ts`                        |
| Test file (.NET)      | matches subject  | `CustomerTests.cs`                             |
| Test file (FE)        | colocated        | `CustomerForm.test.tsx`                        |
| SQL migration         | EF auto + name   | `20260502_Add_Customer_Table.cs`               |
| Markdown docs         | `kebab-case.md`  | `solution-overview.md`                         |
| ADR                   | `adr-NNN-...md`  | `adr-001-clean-architecture.md`                |
