## Summary
<1–2 sentences: what this changes and why.>

## Changes
- <bullet 1>
- <bullet 2>
- <bullet 3>

## Screenshots / Demo
<For UI changes; mark "N/A" otherwise.>

## Test plan
- [ ] Unit tests added/updated
- [ ] Integration tests added/updated
- [ ] MultiTenancy tests added/updated (if touching tenant-scoped data)
- [ ] Manual testing notes:
  - <what you tried locally>

## Convention checklist
- [ ] Code follows `docs/conventions/`
- [ ] Validators added for new commands; handlers `sealed`; structured logs
- [ ] No `Task.Result` / `.Wait()` / `async void` / `catch (Exception)` swallows
- [ ] Strongly-typed IDs, `IClock`, `Guid.CreateVersion7()`
- [ ] EF Core: `IEntityTypeConfiguration<T>` (not inline `modelBuilder.Entity<T>()`)
- [ ] Database: migration name descriptive; reviewed for destructive changes
- [ ] Database: explicit constraint names (`pk_*`, `fk_*`, `ux_*`, `ix_*`, `ck_*`, `rls_*`)
- [ ] Tenant-scoped table: `tenant_id` column + RLS policy + leading-`tenant_id` index
- [ ] API: `[RequirePermission("...")]` or `[AllowAnonymous]` on every endpoint
- [ ] API: `Produces<T>` / `ProducesProblem` documented
- [ ] OpenAPI updated (or auto-generated cleanly); frontend client regen flagged in PR description
- [ ] `X-Idempotency-Key` honored on new financial mutations
- [ ] No secrets, tokens, or PII in code, logs, or error messages
- [ ] Modules communicate only via MediatR domain events (NetArchTest will fail otherwise)

## Risks / Considerations
<What could go wrong? Backwards compatibility? Performance? Anything reviewers should think about?>

## Rollback plan
<How to roll this back. "Revert PR" is acceptable for code-only changes; migrations need more care — call them out.>

## Related
- Closes #<issue>
- Refs <ticket-id>
- Builds on #<other-pr> (if stacked)
