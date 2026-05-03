# ADR Template

Copy to `docs/decisions/adr-NNN-<short-name>.md` for every significant decision. Number sequentially.

---

```markdown
# ADR-NNN: <Short, decisive title>

- **Status**: Proposed | Accepted | Deprecated | Superseded by ADR-XXX
- **Date**: YYYY-MM-DD
- **Decider(s)**: <names / roles>
- **Tags**: <e.g. backend, security, database, frontend>

## Context

What is the problem? What constraints apply? What's been observed in production / in design / in user research that motivates this?

State the question being answered. Be precise — vague context produces vague decisions.

## Options Considered

### Option A: <name>

- **Pros**: ...
- **Cons**: ...
- **Cost / effort**: ...

### Option B: <name>

- **Pros**: ...
- **Cons**: ...
- **Cost / effort**: ...

### Option C: <name>

- **Pros**: ...
- **Cons**: ...
- **Cost / effort**: ...

## Decision

We chose **Option <X>**.

State the choice in one sentence, then a paragraph or two explaining the reasoning. Don't restate every pro/con — focus on what made this option better than the others *for this project*.

## Consequences

### Positive

- ...
- ...

### Negative / Trade-offs

- ...
- ...

### Neutral / Operational

- We must update <X> in CI.
- New engineers need to know <Y>.
- Rollback path: <if/how>.

## Implementation Notes

- Affected components: ...
- Migration plan: ...
- Timeline: ...

## Review / Re-evaluation

This decision should be revisited when:
- <trigger condition 1>
- <trigger condition 2>

Earliest review: YYYY-MM-DD.

## References

- <link to issue / ticket>
- <link to discussion / design doc>
- <link to library / spec>
```

---

## Example: ADR-001 (Worked Example)

```markdown
# ADR-001: Use Clean Architecture for Backend

- **Status**: Accepted
- **Date**: 2026-05-02
- **Decider(s)**: <Architect>, <Lead Engineer>
- **Tags**: backend, architecture

## Context

We need a default backend architecture for new business applications. The team has experience with both transaction-script-style code and layered architectures. Past projects accumulated business logic in services and controllers, leading to long methods, poor testability, and entanglement with EF Core.

## Options Considered

### Option A: Layered (controller → service → repo → DB)
- Pros: familiar; minimal ceremony.
- Cons: business logic drifts into services; hard to unit-test without DB.

### Option B: Clean Architecture + CQRS via MediatR
- Pros: Domain isolated; per-handler tests; clear dependency direction.
- Cons: more files per feature; a learning curve for engineers new to CQRS.

### Option C: Vertical-slice (no layering, one folder per feature)
- Pros: minimal coupling between features; everything for one feature is co-located.
- Cons: shared concepts (DbContext, validators) end up duplicated or in awkward shared folders; harder to enforce direction at scale.

## Decision

We chose **Option B**: Clean Architecture + CQRS via MediatR.

The Domain layer has zero external dependencies — it's pure C# and easy to test exhaustively. CQRS handlers stay small and focused; the MediatR pipeline gives us validation, logging, and performance behaviors for free. The added file count is justified by the predictability and testability gains across many modules.

## Consequences

### Positive
- Domain logic testable without a database.
- Pipeline behaviors centralize cross-cutting concerns.
- Architecture tests (NetArchTest) prevent layering accidents.

### Negative / Trade-offs
- More files per feature than transaction-script style.
- New engineers need a CQRS primer (one afternoon).

### Neutral / Operational
- CI runs NetArchTest on every PR.
- Each module ships with a Domain unit test project.

## Implementation Notes

- Solution layout in `01-backend/01-solution-structure.md`.
- Layer rules in `00-foundation/03-architecture-overview.md`.
- Sample handler in `01-backend/03-application-layer.md` §4.

## Review / Re-evaluation

Revisit if:
- Team size drops below 3 engineers (fewer files might matter more).
- We find ourselves writing > 20% of handlers as pass-through (signals over-engineering).

Earliest review: 2027-05.

## References

- Robert Martin, *Clean Architecture*, 2017.
- Steve Smith, *Clean Architecture with .NET* (Microsoft Learn).
```
