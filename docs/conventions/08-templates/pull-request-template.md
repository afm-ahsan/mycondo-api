# Pull Request Template

Copy to `.github/pull_request_template.md` in your repo.

---

```markdown
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
- [ ] E2E tests added/updated (if user-facing)
- [ ] Manual testing notes:
  - <what you tried locally>

## Convention checklist
- [ ] Code follows `docs/conventions/`
- [ ] Backend: validators added for new commands; handlers `sealed`; structured logs
- [ ] Backend: no `Task.Result` / `.Wait()` / `async void` / `catch (Exception)` swallows
- [ ] Frontend: no `any`, no class components, server state in RTK Query (not slices)
- [ ] Frontend: forms validated by Zod; server-side errors mapped to `form.setError`
- [ ] Database: migration name descriptive; comment block at the top; reviewed for destructive changes
- [ ] Database: explicit constraint names (`pk_*`, `fk_*`, `uix_*`, `ix_*`)
- [ ] API: `Produces<T>` / `ProducesProblem` on every endpoint; permission claim correct
- [ ] Security: no secrets, tokens, or PII in code, logs, or env vars exposed to the browser
- [ ] OpenAPI updated (or auto-generated cleanly); frontend types regenerated if backend contract changed
- [ ] Idempotency-Key on new POST/DELETE mutations

## Risks / Considerations
<What could go wrong? Backwards compatibility? Performance? Anything reviewers should think about?>

## Rollback plan
<How to roll this back if it breaks production. "Revert PR" is acceptable for code-only changes; migrations need more care.>

## Related
- Closes #<issue>
- Refs <ticket-id>
- Builds on #<other-pr> (if stacked)
```
