# Git Workflow and Pull Requests

This document defines branching, commit messages, PR shape, and merge process.

---

## 1. Branching Strategy

We use **trunk-based development with short-lived feature branches.**

```
main (always deployable, protected)
  ├── feat/<short-name>           ← short-lived, < 3 days ideally
  ├── fix/<short-name>
  ├── chore/<short-name>
  └── release/<version>           ← when tagging a release
```

### Rules

- **`main` is always deployable.** Every commit on main goes through CI green.
- **No long-lived feature branches.** Merge to main as soon as a feature is shippable behind a flag.
- **Short branch names**: `feat/customer-list`, `fix/quotation-totals`. Not `feat/12345-add-customer-list-page-and-form-with-validation`.
- **One concern per branch.** A branch is one feature, one fix, one chore — not three.

---

## 2. Conventional Commits

Commit messages follow [Conventional Commits](https://www.conventionalcommits.org/):

```
<type>(<scope>): <short description>

<optional body>

<optional footer(s)>
```

### Types

| Type       | Use for                                              |
|------------|------------------------------------------------------|
| `feat`     | New feature                                          |
| `fix`      | Bug fix                                              |
| `refactor` | Code change that neither fixes a bug nor adds a feature |
| `perf`     | Performance improvement                              |
| `test`     | Adding or fixing tests                               |
| `docs`     | Documentation only                                   |
| `chore`    | Build / tooling / dependency updates                 |
| `style`    | Formatting, missing semicolons (rare)                |
| `ci`       | CI/CD changes                                        |
| `revert`   | Reverting a previous commit                          |

### Scope

Optional. Use the module / area:

- `feat(customers): add list page`
- `fix(auth): refresh token rotation`
- `chore(deps): bump efcore from 10.0.1 to 10.0.2`

### Examples

```
feat(quotations): convert quotation to sales order

Adds POST /api/quotations/{id}/convert-to-sales-order which creates
a SalesOrder from an accepted Quotation and marks the Quotation
as Converted.

Closes #142
```

```
fix(billing): handle null tax rate in invoice total

Previously a missing tax rate caused a NullReferenceException at
invoice generation time. Default to 0% when not specified.

Refs: SR-208
```

```
refactor(customers)!: rename Customer.DisplayName to FullName

BREAKING CHANGE: API contract field "displayName" is now "fullName".
Frontend updated. v1 endpoints remain for 90 days.
```

### Rules

- **Imperative mood.** "Add feature" not "Added feature" or "Adds feature".
- **Subject ≤ 72 characters.**
- **Body explains *why*, not *what*.**
- **`!` after type/scope** signals a breaking change. Include `BREAKING CHANGE:` in the footer.
- **Reference issue tickets** in the footer (`Closes #142`, `Refs: SR-208`).

---

## 3. Pull Request Process

### When to open a PR

- **Open early.** Mark as **Draft** if not ready for review. Early visibility surfaces direction issues.
- **Keep PRs small.** Aim for < 400 lines of diff. Bigger PRs fragment review attention.

### PR title

Same format as commit message:

```
feat(customers): add list page with search and pagination
```

### PR description (template)

A `pull_request_template.md` lives in `.github/`. See `08-templates/`.

### Review

- **At least 1 approver** required by branch protection.
- **Reviewer checks against the relevant convention checklist** (backend / frontend / security).
- **Comments are actionable.** "I don't like this" is not feedback. "This duplicates `formatCurrency` — can we use that instead?" is.
- **Conversations resolved by the author** when addressed; the reviewer can re-open if not satisfied.

### Merging

- **Squash-merge to main.** Keeps history clean.
- **The squash commit message** is curated by the merger — usually the PR title + the meaningful body.
- **Branch deleted** after merge.
- **No merge commits on main.** Linear history is enforced by branch protection.

### Rules

- **Don't ping reviewers without context.** Mention what changed if you've pushed updates.
- **Reviewer aims for < 24-hour SLA** during business hours.
- **Author addresses comments before re-requesting review.**

---

## 4. PR Description Template

```markdown
## Summary
<1-2 sentences: what this changes and why>

## Changes
- <bullet 1>
- <bullet 2>

## Screenshots / Demo
<if UI changes; mark "N/A" otherwise>

## Test plan
- [ ] Unit tests added/updated
- [ ] Integration tests added/updated
- [ ] E2E tests added/updated (if user-facing)
- [ ] Manual testing notes:
  - <what you tried>

## Checklist
- [ ] Code follows conventions in `docs/conventions/`
- [ ] No secrets, tokens, or PII committed or logged
- [ ] Migrations: reviewed for destructive changes; rollback documented (or N/A)
- [ ] OpenAPI updated (if endpoints changed)
- [ ] Frontend types regenerated from OpenAPI (if backend contract changed)

## Related
- Closes #<issue>
- Refs <ticket-id>
```

---

## 5. Issue Tracking

Issues live in GitHub Issues (or the team's tracker — Linear, Jira, ...). Each issue has:

- **Title**: concise problem statement.
- **Description**: context, acceptance criteria, screenshots if relevant.
- **Labels**: type (bug / feat / chore), priority, area.
- **Assignee**: one person owns it.

### Rules

- **Every PR references an issue** — except trivial fixes (typos, formatting).
- **Issues closed via PR merge** through the `Closes #<n>` keyword.

---

## 6. Code Review Standards

The reviewer checks:

### Architecture
- Does this respect Clean Architecture layering?
- Are aggregates the right boundary?
- Is the right module the home for this code?

### Correctness
- Does the code do what the description claims?
- Edge cases considered (empty lists, null inputs, concurrent writes)?
- Error paths typed and tested?

### Quality
- Naming clear and consistent?
- No dead code, no commented-out code?
- No `// TODO` without a ticket reference?

### Security
- New endpoint authorized?
- Inputs validated?
- No PII / secrets in logs?

### Performance
- N+1 query risk?
- Index supports the query?
- Cache invalidation correct?

### Tests
- New unit tests for new logic?
- Integration test for new endpoints?
- E2E test for new user flows?

### Rules

- **The reviewer is the guard rail.** Authors miss things; that's fine — that's why we have review.
- **Reviewer doesn't owe the author approval.** It's not a polite ritual. Suggest changes when needed.
- **Disagreements escalate to the team** — not silent merging.

---

## 7. Reverting

When a merge breaks something:

```bash
git revert <commit-sha>
git push origin main
```

### Rules

- **Revert first, investigate later.** A broken main is more expensive than a slightly messier history.
- **Don't force-push to main.** Branch protection should prevent this anyway.
- **A revert is a normal commit.** Goes through CI like any other.

---

## 8. Hotfixes

For urgent production issues:

```bash
git checkout main
git pull
git checkout -b fix/<short-name>
# ...make fix...
git commit -am "fix(scope): description"
git push
# Open PR with [HOTFIX] tag in title; expedited review
```

### Rules

- **Even hotfixes go through PR.** Two pairs of eyes on a 2 AM fix is worth the 5-minute delay.
- **Tests included.** A hotfix that didn't add a regression test is a hotfix that may recur.
- **Cherry-pick to release branches** if maintaining multiple versions.

---

## 9. Release Notes

Generated from Conventional Commits:

```
## v1.4.0 (2026-05-15)

### Features
- (customers) add list page with search and pagination (#142)
- (quotations) convert quotation to sales order (#147)

### Bug Fixes
- (billing) handle null tax rate in invoice total (#150)

### Chores
- (deps) bump efcore from 10.0.1 to 10.0.2 (#151)
```

Tools: `release-please`, `semantic-release`, or simply `git log --pretty='format:- %s'` with manual cleanup.

---

## 10. Common Mistakes

| Mistake                                                          | Fix                                                              |
|------------------------------------------------------------------|------------------------------------------------------------------|
| Long-lived feature branches that diverge for weeks                | Merge to main behind a flag; rebase often                        |
| PRs > 1000 lines                                                  | Split into reviewable chunks                                     |
| Commit messages like "fix stuff" or "WIP"                         | Conventional Commits; meaningful subjects                        |
| Force-push to main                                                | Branch protection; revert via new commit                         |
| Merging without review                                            | Required reviewer in branch protection                           |
| Bundled refactor + feature in one PR                              | Separate PRs                                                     |
| "Just fix it later" TODO without a ticket                         | `// TODO(<TICKET>):` or remove                                   |
| Reviewing only the diff, ignoring the architecture                | Open the file, look at the whole context                         |
| Author resolves their own comments without addressing             | Reviewer reopens                                                 |
| Reverting via "git reset --hard"                                  | Use `git revert` so history is preserved                         |
