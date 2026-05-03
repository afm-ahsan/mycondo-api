# PostgreSQL Naming Convention

**Database**: PostgreSQL 16+ (uses `tstzrange`, GIST exclusion, generated columns, JSONB, identity, UUIDv7).
**ORM**: EF Core 10 with Npgsql provider — snake_case via `EFCore.NamingConventions`.

> Apply this to **every** PostgreSQL object: tables, columns, indexes, constraints, sequences, views, functions, schemas, roles. Migrations that violate these rules fail CI.

---

## 1. Universal Rules

| Rule                     | Value                                                      |
|--------------------------|------------------------------------------------------------|
| Case                     | `snake_case` for everything (no quoted identifiers)        |
| Maximum length           | 63 characters (PostgreSQL hard limit)                      |
| Reserved words           | Forbidden (e.g. `user`, `order`, `group`, `count`)         |
| Plurality — tables       | **Singular** (`customer`, not `customers`)                 |
| Plurality — junction     | Singular concatenated (`user_role`, `customer_tag`)        |
| Plurality — collections  | Plural in views/functions only (`v_active_customers`)      |
| Quoted identifiers       | Never use them in DDL or queries                           |
| String type              | `text` (not `varchar(n)`) unless a real upper bound exists |
| Timestamp type           | **Always** `timestamptz`. Never `timestamp` (no TZ)        |

### Why singular tables?

A row in `customer` *is* a customer — singular reads naturally. Aggregate names in the Domain layer (`Customer`, `Invoice`) align 1:1 with table names.

### Reserved words

Many natural domain terms are SQL reserved words: `user`, `order`, `group`, `role`, `count`, `type`. Either:
- **Pick a synonym** (`account` for user, `customer_order` for order, `team` for group, `permission_role` for role) — **preferred**.
- Accept the word and adjust at the application layer.

---

## 2. Schemas

Use schemas to separate concerns. Avoid dumping everything in `public`.

| Schema       | Purpose                                                    |
|--------------|------------------------------------------------------------|
| `app`        | Core business tables                                       |
| `auth`       | Identity / authentication tables (users, roles, claims)    |
| `audit`      | Audit log, history tables                                  |
| `outbox`     | Outbox + processed-message tracking                        |
| `analytics`  | Materialized views, denormalized read models               |

For modular monoliths, give each bounded context its own schema: `<context>` (e.g. `billing`, `inventory`, `notifications`).

```sql
CREATE SCHEMA app;
CREATE SCHEMA auth;
CREATE SCHEMA audit;
CREATE SCHEMA outbox;
SET search_path TO app, public;
```

---

## 3. Tables

| Pattern                               | Example                                          |
|---------------------------------------|--------------------------------------------------|
| Singular noun, snake_case             | `customer`, `invoice`, `quotation`               |
| Junction (many-to-many)               | `<a>_<b>` alphabetical (`role_user`, `customer_tag`) |
| Lookup / reference                    | `<entity>_status`, `<entity>_type`               |
| Audit / history                       | `<table>_history` or in `audit` schema           |
| Outbox messages                       | `outbox_message`, `outbox_message_processed`     |
| Materialized views                    | `mv_<purpose>` → `mv_account_balance`            |
| Regular views                         | `v_<purpose>` → `v_active_invoice`               |

**Forbidden:** `tbl_*`, `t_*`, or any Hungarian-notation prefix.

---

## 4. Columns

### Core rules

| Pattern                                  | Example                              |
|------------------------------------------|--------------------------------------|
| `snake_case`                             | `created_at_utc`, `is_active`        |
| Primary key                              | `id` (always — never `<table>_id` for the table's own PK) |
| Foreign key                              | `<referenced_table>_id`              |
| Boolean                                  | Prefix `is_`, `has_`, `can_`         |
| Timestamps (UTC)                         | Suffix `_at_utc`                     |
| Audit timestamps                         | `created_at_utc`, `updated_at_utc`, `deleted_at_utc` |
| Audit user fields                        | `created_by`, `updated_by` (FK to user.id) |
| Soft delete flag                         | `deleted_at_utc` (preferred)         |
| Concurrency token                        | `version` (int, increments on every UPDATE) |
| Money / amounts                          | `numeric(19, 4)` — never `float`/`real`      |
| Enums                                    | Stored as `text` with CHECK, OR PG `ENUM` type |
| JSON                                     | `jsonb` (never `json`); column suffix `_json` |
| Sequence/Identity                        | `GENERATED ALWAYS AS IDENTITY` for surrogate ints; UUIDv7 for distributed |

### Suffix reference

| Suffix     | Meaning                                | Example                |
|------------|----------------------------------------|------------------------|
| `_id`      | Foreign key                            | `customer_id`          |
| `_at_utc`  | Point-in-time UTC                      | `submitted_at_utc`     |
| `_on`      | Calendar date (no time)                | `birth_date_on`        |
| `_count`   | Cardinality / running total            | `attempt_count`        |
| `_amount`  | Monetary value                         | `total_amount`         |
| `_status`  | State enum                             | `order_status`         |
| `_type`    | Discriminator                          | `payment_type`         |
| `_json`    | JSONB column                           | `metadata_json`        |

### Forbidden names

- `desc`, `order`, `user`, `group`, `count`, `type` (without context), `data`, `value` (no qualifier).
- Hungarian prefixes: `str_*`, `int_*`, `b_*`.
- Plural columns unless the column truly holds multiple values (`tags text[]`).

---

## 5. Primary and Foreign Keys

### Primary keys

```sql
CREATE TABLE app.customer (
    id          uuid PRIMARY KEY,                        -- UUIDv7 from app
    ...
);
```

- **Aggregate roots use UUIDv7**, generated client-side: `Guid.CreateVersion7()` in C#.
- **Lookup tables with stable, small cardinality** may use `int` identity:
  ```sql
  id int GENERATED ALWAYS AS IDENTITY PRIMARY KEY
  ```

### Foreign keys

```sql
ALTER TABLE app.invoice
  ADD CONSTRAINT fk_invoice_customer
  FOREIGN KEY (customer_id) REFERENCES app.customer (id)
  ON DELETE RESTRICT ON UPDATE NO ACTION;
```

### Rules

- **Default action: `ON DELETE RESTRICT`** (or `NO ACTION`). `CASCADE` is allowed only with explicit justification in the migration comment.
- **Every FK gets an index** (FKs are not auto-indexed in PostgreSQL).
- **`ON UPDATE NO ACTION`** — we don't use updatable PKs.

---

## 6. Constraints

Constraints have **explicit names**. PostgreSQL-generated names are forbidden.

| Type                  | Pattern                              | Example                                          |
|-----------------------|--------------------------------------|--------------------------------------------------|
| Primary key           | `pk_<table>`                         | `pk_customer`                                    |
| Foreign key           | `fk_<table>_<referenced_table>`      | `fk_invoice_customer`                            |
| Unique                | `uq_<table>_<columns>`               | `uq_user_email`                                  |
| Check                 | `ck_<table>_<rule>`                  | `ck_invoice_amount_positive`                     |
| Exclusion             | `ex_<table>_<rule>`                  | `ex_amc_no_overlap`                              |
| Default               | `df_<table>_<column>`                | `df_invoice_status`                              |

### Examples

```sql
-- Unique business key
ALTER TABLE auth.user
  ADD CONSTRAINT uq_user_email UNIQUE (email);

-- Domain rule via CHECK
ALTER TABLE app.invoice
  ADD CONSTRAINT ck_invoice_amount_positive CHECK (total_amount > 0);

-- No overlapping time-bounded ranges per parent
-- Useful for: schedules, leases, AMC contracts
CREATE EXTENSION IF NOT EXISTS btree_gist;

ALTER TABLE app.amc_contract
  ADD CONSTRAINT ex_amc_no_overlap
  EXCLUDE USING gist (
    customer_id WITH =,
    tstzrange(start_at_utc, end_at_utc) WITH &&
  ) WHERE (status <> 'Cancelled');
```

---

## 7. Indexes

| Pattern                                 | Example                                                  |
|-----------------------------------------|----------------------------------------------------------|
| B-tree (default)                        | `ix_<table>_<columns>` → `ix_invoice_customer_status_date` |
| Unique index (not a constraint)         | `uix_<table>_<columns>`                                  |
| Partial                                 | `ix_<table>_<columns>__<predicate>` → `ix_invoice_customer__unpaid` |
| Covering (`INCLUDE`)                    | `ix_<table>_<key_cols>__inc_<inc_cols>`                  |
| GIN/GIST                                | `gin_<table>_<column>`, `gist_<table>_<column>`          |
| BRIN (large append-only tables)         | `brin_<table>_<column>`                                  |

### Rules

- **Order columns** in compound indexes from **most-selective to least-selective**, with equality-predicate columns first.
- **Always include the predicate** in partial-index names (after `__`) for self-documentation.
- **Drop unused indexes** after 30 days in production (validated via `pg_stat_user_indexes`).

### Common indexes worth considering

```sql
-- Lookup hot path
CREATE INDEX ix_invoice_customer_status_date
  ON app.invoice (customer_id, status, created_at_utc);

-- History pagination
CREATE INDEX ix_audit_log_actor_created
  ON audit.audit_log (actor_id, created_at_utc DESC);

-- Outbox dispatch
CREATE INDEX ix_outbox_message_unprocessed
  ON outbox.outbox_message (occurred_at_utc)
  WHERE processed_at_utc IS NULL;

-- Full-text-ish search (use pg_trgm)
CREATE EXTENSION IF NOT EXISTS pg_trgm;
CREATE INDEX gin_customer_name_trgm
  ON app.customer USING gin (name gin_trgm_ops);
```

---

## 8. Sequences and Identity

- Prefer `GENERATED ALWAYS AS IDENTITY` over `SERIAL` (SQL standard).
- Sequence naming (when explicit): `<table>_<column>_seq` (auto-generated default; don't override unless required).

---

## 9. Functions, Procedures, Triggers

| Object              | Pattern                       | Example                              |
|---------------------|-------------------------------|--------------------------------------|
| Function            | `fn_<verb>_<noun>`            | `fn_calculate_invoice_total`         |
| Procedure           | `sp_<verb>_<noun>`            | `sp_archive_old_quotation`           |
| Trigger             | `tg_<table>_<event>_<action>` | `tg_invoice_before_update_audit`     |
| Trigger function    | `fn_tg_<table>_<purpose>`     | `fn_tg_invoice_audit`                |

### Rules

- **Use functions sparingly.** Most logic belongs in the application layer.
- **Triggers only for invariants the app cannot enforce** (e.g. cross-row exclusion that belongs at the DB level).
- **Document every trigger** with a comment explaining what and why.

---

## 10. EF Core Mapping

Configure the DbContext **once** to enforce snake_case automatically:

```csharp
optionsBuilder
    .UseNpgsql(connectionString)
    .UseSnakeCaseNamingConvention();   // EFCore.NamingConventions package
```

This converts `Invoice.TotalAmount` (C#) → `invoice.total_amount` (PostgreSQL) without per-property mapping.

For constraints/indexes EF auto-generates (e.g. FK indexes), override names in `IEntityTypeConfiguration<T>`:

```csharp
public sealed class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.ToTable("invoice", schema: "app");
        builder.HasKey(x => x.Id).HasName("pk_invoice");

        builder.HasIndex(x => new { x.CustomerId, x.Status, x.CreatedAtUtc })
               .HasDatabaseName("ix_invoice_customer_status_date");

        builder.Property(x => x.Version).IsConcurrencyToken();
    }
}
```

---

## 11. Standard Aggregate Template

Every aggregate root table follows this base shape:

```sql
CREATE TABLE app.<aggregate> (
    id              uuid PRIMARY KEY,
    -- ... domain columns ...
    is_active       boolean NOT NULL DEFAULT true,
    created_at_utc  timestamptz NOT NULL DEFAULT now(),
    updated_at_utc  timestamptz NOT NULL DEFAULT now(),
    created_by      uuid,
    updated_by      uuid,
    deleted_at_utc  timestamptz,
    version         integer NOT NULL DEFAULT 1
);
```

### Rules

- **Audit columns are universal** — every business table has them.
- **`version` is the optimistic-concurrency token.** Configured in EF Core via `IsConcurrencyToken()`.
- **`deleted_at_utc` is the soft-delete column.** When set, the row is logically deleted.

---

## 12. Audit Schema (For Regulated Data)

If the project handles regulated data (PHI, PII):

```sql
CREATE TABLE audit.audit_log (
    id                uuid PRIMARY KEY,
    occurred_at_utc   timestamptz NOT NULL DEFAULT now(),
    actor_id          uuid,                              -- who did it
    correlation_id    uuid,                              -- request correlation
    schema_name       text NOT NULL,
    table_name        text NOT NULL,
    record_id         uuid NOT NULL,
    operation         text NOT NULL,                     -- 'INSERT' | 'UPDATE' | 'DELETE'
    before_json       jsonb,
    after_json        jsonb,
    diff_json         jsonb,                             -- generated column
    CONSTRAINT ck_audit_log_operation CHECK (operation IN ('INSERT','UPDATE','DELETE'))
);

CREATE INDEX ix_audit_log_table_record
  ON audit.audit_log (schema_name, table_name, record_id, occurred_at_utc DESC);

CREATE INDEX ix_audit_log_actor
  ON audit.audit_log (actor_id, occurred_at_utc DESC);
```

### Rules

- **Append-only.** No UPDATE or DELETE on `audit_log`. Only retention jobs prune.
- **Triggers populate it.** App-level audit also acceptable; pick one and stick to it.
- **Detail in** `03-database/04-audit-and-soft-delete.md`.

---

## 13. Outbox Schema

```sql
CREATE TABLE outbox.outbox_message (
    id                uuid PRIMARY KEY,
    occurred_at_utc   timestamptz NOT NULL DEFAULT now(),
    aggregate_type    text NOT NULL,
    aggregate_id      uuid NOT NULL,
    event_type        text NOT NULL,
    payload_json      jsonb NOT NULL,
    correlation_id    uuid,
    processed_at_utc  timestamptz,
    error             text,
    attempt_count     integer NOT NULL DEFAULT 0
);

CREATE INDEX ix_outbox_message_unprocessed
  ON outbox.outbox_message (occurred_at_utc)
  WHERE processed_at_utc IS NULL;
```

---

## 14. Healthcare / Regulated Notes

If the project handles PHI / PII / financial data:

- **Audit every read** of sensitive tables (queryable trail).
- **Encrypt at-rest sensitive columns** via `pgcrypto` or column-level encryption from the app layer.
- **Soft-delete only** for retention-bound records — no hard deletes from app code.
- **Row-Level Security (RLS)** policies on tables shared across tenants/orgs:
  ```sql
  ALTER TABLE app.invoice ENABLE ROW LEVEL SECURITY;
  CREATE POLICY rls_invoice_tenant
    ON app.invoice FOR ALL TO app_user
    USING (tenant_id = current_setting('app.tenant_id')::uuid);
  ```
- **Document column sensitivity** in migration comments.

---

## 15. Common Mistakes

| Mistake                                                     | Fix                                                                |
|-------------------------------------------------------------|--------------------------------------------------------------------|
| Plural table names (`customers`)                            | Singular (`customer`)                                              |
| Quoted identifiers (`"Customer"`)                           | snake_case + `EFCore.NamingConventions`                            |
| `timestamp` columns (no TZ)                                 | `timestamptz`, suffix `_at_utc`                                    |
| `varchar(50)` for everything                                | `text` unless a true business limit exists                         |
| `ON DELETE CASCADE` everywhere                              | `RESTRICT` by default; `CASCADE` requires justification            |
| EF auto-generated constraint names                          | Explicit `pk_*`, `fk_*`, `uix_*`, `ix_*`                           |
| Missing index on FK column                                  | Always add an index                                                |
| Storing money as `float`                                    | `numeric(19, 4)`                                                   |
| Storing JSON as `json` instead of `jsonb`                   | Always `jsonb`                                                     |
| Mixed-schema queries without prefix                         | Always prefix: `app.customer`, `outbox.outbox_message`             |
