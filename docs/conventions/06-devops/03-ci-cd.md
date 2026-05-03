# CI/CD with GitHub Actions

This document defines pipelines for backend and frontend: lint → typecheck → test → build → scan → push image → deploy.

---

## 1. Repository Structure

```
.github/
└── workflows/
    ├── backend.yml          # build + test + deploy backend
    ├── frontend.yml         # build + test + deploy frontend
    ├── codeql.yml           # static security analysis
    └── pr-checks.yml        # PR-only checks (commit lint, etc.)
```

---

## 2. Backend Workflow

```yaml
# .github/workflows/backend.yml
name: backend

on:
  push:
    branches: [ main ]
    paths:
      - '<Project>.Core/**'
      - '.github/workflows/backend.yml'
  pull_request:
    paths:
      - '<Project>.Core/**'

defaults:
  run:
    working-directory: <Project>.Core

env:
  DOTNET_NOLOGO: 'true'
  DOTNET_CLI_TELEMETRY_OPTOUT: 'true'

jobs:
  build-test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'

      - name: Restore
        run: dotnet restore

      - name: Build
        run: dotnet build --no-restore -c Release

      - name: Test
        run: dotnet test --no-build -c Release \
             --collect:"XPlat Code Coverage" \
             --logger:"trx;LogFileName=test-results.trx" \
             --results-directory ./test-results

      - name: Upload test results
        if: always()
        uses: actions/upload-artifact@v4
        with:
          name: backend-test-results
          path: <Project>.Core/test-results/

      - name: Upload coverage
        if: success()
        uses: codecov/codecov-action@v4
        with:
          files: <Project>.Core/test-results/**/coverage.cobertura.xml
          flags: backend

  publish:
    needs: build-test
    if: github.event_name == 'push' && github.ref == 'refs/heads/main'
    runs-on: ubuntu-latest
    permissions:
      contents: read
      packages: write
    steps:
      - uses: actions/checkout@v4

      - uses: docker/setup-buildx-action@v3

      - uses: docker/login-action@v3
        with:
          registry: ghcr.io
          username: ${{ github.actor }}
          password: ${{ secrets.GITHUB_TOKEN }}

      - name: Extract metadata
        id: meta
        uses: docker/metadata-action@v5
        with:
          images: ghcr.io/${{ github.repository }}/api
          tags: |
            type=sha,prefix=,suffix=,format=short
            type=raw,value=latest,enable={{is_default_branch}}

      - name: Build and push
        uses: docker/build-push-action@v5
        with:
          context: <Project>.Core
          file: <Project>.Core/Dockerfile
          push: true
          tags: ${{ steps.meta.outputs.tags }}
          labels: ${{ steps.meta.outputs.labels }}
          cache-from: type=gha
          cache-to: type=gha,mode=max

      - name: Trivy scan
        uses: aquasecurity/trivy-action@master
        with:
          image-ref: ghcr.io/${{ github.repository }}/api:${{ github.sha }}
          format: sarif
          output: trivy.sarif
          severity: CRITICAL,HIGH
          exit-code: '1'

      - name: Upload Trivy results
        if: always()
        uses: github/codeql-action/upload-sarif@v3
        with:
          sarif_file: trivy.sarif
```

### Rules

- **Path filters** so backend changes don't trigger frontend pipelines and vice versa.
- **Cache-aware build** (`type=gha`) — fast incremental builds.
- **Trivy scan fails the build** on Critical/High.
- **Image tagged with short SHA**; `latest` tag only on `main`.

---

## 3. Frontend Workflow

```yaml
# .github/workflows/frontend.yml
name: frontend

on:
  push:
    branches: [ main ]
    paths:
      - '<Project>.Client/**'
      - '.github/workflows/frontend.yml'
  pull_request:
    paths:
      - '<Project>.Client/**'

defaults:
  run:
    working-directory: <Project>.Client

jobs:
  build-test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - uses: actions/setup-node@v4
        with:
          node-version: '22'
          cache: 'npm'
          cache-dependency-path: <Project>.Client/package-lock.json

      - run: npm ci --no-audit --no-fund

      - run: npm run typecheck
      - run: npm run lint
      - run: npm test -- --coverage

      - run: npm run build
        env:
          VITE_API_BASE_URL: ${{ vars.VITE_API_BASE_URL }}

      - name: Upload coverage
        if: success()
        uses: codecov/codecov-action@v4
        with:
          files: <Project>.Client/coverage/lcov.info
          flags: frontend

  e2e:
    needs: build-test
    runs-on: ubuntu-latest
    services:
      postgres:
        image: postgres:16-alpine
        env:
          POSTGRES_USER: app
          POSTGRES_PASSWORD: app
          POSTGRES_DB: app
        ports: ['5432:5432']
        options: >-
          --health-cmd pg_isready
          --health-interval 10s
          --health-timeout 5s
          --health-retries 5
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-node@v4
        with: { node-version: '22', cache: 'npm', cache-dependency-path: <Project>.Client/package-lock.json }
      - run: npm ci

      - name: Start backend (compose)
        working-directory: ./
        run: docker compose -f compose.e2e.yml up -d --wait

      - run: npx playwright install --with-deps chromium firefox
      - run: npm run test:e2e
        env:
          E2E_BASE_URL: http://localhost:5173

      - name: Upload Playwright report on failure
        if: failure()
        uses: actions/upload-artifact@v4
        with:
          name: playwright-report
          path: <Project>.Client/playwright-report/

  publish:
    needs: [build-test, e2e]
    if: github.event_name == 'push' && github.ref == 'refs/heads/main'
    runs-on: ubuntu-latest
    permissions: { contents: read, packages: write }
    steps:
      - uses: actions/checkout@v4
      - uses: docker/setup-buildx-action@v3
      - uses: docker/login-action@v3
        with:
          registry: ghcr.io
          username: ${{ github.actor }}
          password: ${{ secrets.GITHUB_TOKEN }}
      - uses: docker/metadata-action@v5
        id: meta
        with:
          images: ghcr.io/${{ github.repository }}/web
          tags: type=sha,format=short
      - uses: docker/build-push-action@v5
        with:
          context: <Project>.Client
          file: <Project>.Client/Dockerfile
          push: true
          tags: ${{ steps.meta.outputs.tags }}
          build-args: |
            VITE_API_BASE_URL=${{ vars.VITE_API_BASE_URL_PROD }}
          cache-from: type=gha
          cache-to: type=gha,mode=max
```

### Rules

- **Typecheck + lint + unit tests run on every PR.**
- **E2E only on PRs touching frontend** (and on main pushes).
- **Backend service spun up via compose** in the E2E job.
- **Playwright report uploaded on failure** for fast triage.

---

## 4. PR Checks

```yaml
# .github/workflows/pr-checks.yml
name: pr-checks
on: pull_request

jobs:
  commit-messages:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
        with: { fetch-depth: 0 }
      - uses: wagoid/commitlint-github-action@v6

  pr-title:
    runs-on: ubuntu-latest
    steps:
      - uses: amannn/action-semantic-pull-request@v5
        env: { GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }} }

  forbidden-files:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - name: Check for committed secrets
        run: |
          if grep -rEn '(api_key|secret|password|token)\s*=\s*["'\''][^"'\'']{12,}' . \
            --include='*.json' --include='*.cs' --include='*.ts' --include='*.tsx'; then
            echo "Possible secret found"; exit 1
          fi
```

### Rules

- **Commit messages follow Conventional Commits** (see `07-standards/01-git-and-pull-requests.md`).
- **PR titles enforced** to be Conventional Commits style.
- **Secret scan** in CI as a backstop to pre-commit hooks.

---

## 5. CodeQL (Security Static Analysis)

```yaml
# .github/workflows/codeql.yml
name: codeql
on:
  push: { branches: [ main ] }
  pull_request: { branches: [ main ] }
  schedule: [ { cron: '0 6 * * 1' } ]

jobs:
  analyze:
    runs-on: ubuntu-latest
    permissions: { security-events: write, contents: read }
    strategy:
      matrix:
        language: [ csharp, javascript-typescript ]
    steps:
      - uses: actions/checkout@v4
      - uses: github/codeql-action/init@v3
        with: { languages: ${{ matrix.language }} }
      - uses: github/codeql-action/autobuild@v3
      - uses: github/codeql-action/analyze@v3
```

### Rules

- **Runs weekly + on every push.**
- **Findings appear in GitHub Security tab.**
- **High-severity findings break the build via branch-protection.**

---

## 6. Branch Protection (GitHub Settings)

For `main`:
- **Require PR review** (1+ approver).
- **Require status checks**: backend build-test, frontend build-test, frontend e2e, codeql.
- **Require branches up to date** before merge.
- **Require linear history.** No merge commits.
- **Restrict force pushes.**
- **Require signed commits** (recommended).

---

## 7. Deployment

### Continuous deployment to staging

After `main` push:

```yaml
deploy-staging:
  needs: publish
  runs-on: ubuntu-latest
  environment: staging
  steps:
    - uses: actions/checkout@v4
    - name: Deploy
      run: |
        ssh deploy@staging.example.com bash -s <<'EOF'
          set -e
          cd /opt/<project>
          export TAG=${{ github.sha }}
          docker compose -f compose.prod.yml pull
          docker compose -f compose.prod.yml run --rm migrate
          docker compose -f compose.prod.yml up -d --no-deps api web
        EOF
```

### Manual promote to production

A separate workflow that takes a `tag` input:

```yaml
on:
  workflow_dispatch:
    inputs:
      tag:
        description: 'Image tag (SHA) to deploy'
        required: true
```

### Rules

- **Staging is auto-deployed** from main.
- **Production is manually promoted** by re-tagging the same image.
- **Migrations as a separate `compose run --rm migrate` step**, before `up`.
- **Deploys are idempotent.** Re-running shouldn't break anything.
- **Rollback is `docker compose pull` + `up` with the previous tag.**

---

## 8. Release Tagging

When a release is cut:

```bash
git tag v1.4.0
git push origin v1.4.0
```

A `release.yml` workflow creates the GitHub release, tags the Docker image as `v1.4.0`, and updates a changelog.

### Rules

- **Semantic versioning**: `MAJOR.MINOR.PATCH`.
- **Conventional Commits drive automatic changelog generation** (e.g. via `release-please` or `semantic-release`).
- **Tags are immutable.** Never re-tag.

---

## 9. Renovate / Dependabot

`.github/dependabot.yml`:

```yaml
version: 2
updates:
  - package-ecosystem: nuget
    directory: /<Project>.Core
    schedule: { interval: weekly }
    open-pull-requests-limit: 10
  - package-ecosystem: npm
    directory: /<Project>.Client
    schedule: { interval: weekly }
    open-pull-requests-limit: 10
  - package-ecosystem: docker
    directory: /<Project>.Core
    schedule: { interval: weekly }
  - package-ecosystem: github-actions
    directory: /
    schedule: { interval: weekly }
```

### Rules

- **Weekly cadence.** Daily creates noise.
- **Auto-merge minor + patch** when CI passes (via `dependabot-auto-merge` action).
- **Major bumps reviewed manually**, with an ADR if it requires code changes.

---

## 10. Common Mistakes

| Mistake                                                | Fix                                                            |
|--------------------------------------------------------|----------------------------------------------------------------|
| Tests skipped on PRs                                   | Required status check                                          |
| Image tagged only `latest`                             | Always tag with SHA; `latest` only on main                     |
| Deploy directly from `main` to production              | Promote staging-tested image                                   |
| Migrations baked into the app and run at startup       | Separate `migrate` step                                        |
| Cache mismatch between branches                        | Use `cache-from: type=gha` correctly; isolate by branch        |
| Trivy results not surfaced                             | Upload SARIF; show in Security tab                             |
| No path filters — every PR runs everything             | Filter per directory                                           |
| Secrets visible in logs (echoed)                       | Use GitHub Secrets, never `echo` them                          |
| Manual deploy without rollback path                    | Document rollback in runbook                                   |
| `npm install` instead of `npm ci`                      | `ci` uses lockfile; deterministic                              |
