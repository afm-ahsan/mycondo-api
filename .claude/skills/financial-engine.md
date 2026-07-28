---
name: financial-engine
description: Non-negotiable invariants for MyCondo's billing/invoicing/payment/ledger modules. Use before touching anything under Features/Billing, Invoices, Payments, Ledger, or Expenses (not yet built as of Wave 0 — this is the design contract for when they are).
---

# Financial Engine Design Rules

None of this is built yet (Wave 4/5 scope) — this skill exists so the first financial code follows
the rules from the start rather than needing a rewrite.

## Invariants (non-negotiable)

1. Posted financial records are immutable. Corrections = reversal + replacement, never edit-in-place.
2. Every journal entry balances (debits == credits) — enforce this in the aggregate, not just by
   convention.
3. Payment submission and billing generation are both idempotent — require `X-Idempotency-Key` on
   financial mutation endpoints, validated against a stored idempotency-keys table.
4. Invoice numbering is sequential within an approved scope (format TBD — flagged as an open question
   in `kickoff.md` §11, do not invent one unilaterally).
5. Payment allocation is transactional, FIFO, deterministic, under `SELECT ... FOR UPDATE` row
   locking.
6. Overpayments become unapplied credit, never silently discarded or auto-refunded.
7. Voiding an invoice creates reversing entries; the invoice is marked void, never deleted.
8. Money is `numeric` in PostgreSQL — never floating-point.
9. Posting dates and business dates are explicit and distinct fields.
10. Closed financial periods reject mutation.
11. Every financial operation is audited.
12. Every financial command supports concurrency protection (optimistic for the aggregate, row locks
    for allocation).
13. Duplicate execution (retry, double-click, replayed request) must not create duplicate financial
    effects — this is what the idempotency key exists to prevent; test it explicitly.

## The ledger

Write the ledger entry in the **same transaction** as the operational record (invoice, payment) — not
as a secondary reporting process run later. Append-only; no deletes.

## Definition of Done addendum for financial work

Beyond the project-wide DoD: balanced-journal verification, duplicate-execution verification,
concurrency verification, reversal verification, rounding verification, reconciliation verification.
A financial PR without tests for all six is not done.
