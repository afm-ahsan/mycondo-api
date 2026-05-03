# Docker and Compose

Every project ships with **Dockerfiles for backend and frontend** and a **`docker-compose.yml`** that brings up the full local stack (api + web + postgres + redis).

---

## 1. Backend Dockerfile (Multi-Stage)

```dockerfile
# syntax=docker/dockerfile:1.7

# ---- Build stage ----
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

# Copy solution + props for layer caching
COPY *.sln Directory.*.props global.json ./
COPY src/<Project>.Api/*.csproj            src/<Project>.Api/
COPY src/<Project>.Application/*.csproj    src/<Project>.Application/
COPY src/<Project>.Domain/*.csproj         src/<Project>.Domain/
COPY src/<Project>.Infrastructure/*.csproj src/<Project>.Infrastructure/
COPY src/<Project>.Shared/*.csproj         src/<Project>.Shared/
RUN dotnet restore src/<Project>.Api/<Project>.Api.csproj

# Copy the rest and publish
COPY src ./src
RUN dotnet publish src/<Project>.Api/<Project>.Api.csproj \
    -c Release \
    -o /app \
    --no-restore \
    /p:UseAppHost=false

# ---- Runtime stage ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime
WORKDIR /app

# Run as non-root
RUN addgroup -S app && adduser -S app -G app
USER app

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_NOLOGO=1

COPY --from=build --chown=app:app /app .

EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=5s --start-period=15s --retries=3 \
  CMD wget -qO- http://localhost:8080/health/live || exit 1

ENTRYPOINT ["dotnet", "<Project>.Api.dll"]
```

### Rules

- **Multi-stage**: SDK image for build, `aspnet:alpine` for runtime.
- **Layer-cache friendly**: copy `.csproj` first, restore, then copy source.
- **Non-root user** in the runtime image.
- **`HEALTHCHECK`** uses `/health/live` so orchestrators can detect a hung process.
- **`UseAppHost=false`** strips the platform-specific apphost — saves ~100MB.

---

## 2. Frontend Dockerfile (Multi-Stage)

```dockerfile
# ---- Build stage ----
FROM node:22-alpine AS build
WORKDIR /app

COPY package*.json ./
RUN npm ci --no-audit --no-fund

COPY . .

ARG VITE_API_BASE_URL
ENV VITE_API_BASE_URL=${VITE_API_BASE_URL}

RUN npm run build

# ---- Runtime stage ----
FROM nginx:1.27-alpine AS runtime

COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html

EXPOSE 80

HEALTHCHECK --interval=30s --timeout=3s --retries=3 \
  CMD wget -qO- http://localhost/ || exit 1
```

### Rules

- **`VITE_*` build-args** for environment-specific bundles. Different envs = different images (or build at deploy time).
- **nginx serves static**, with SPA fallback.
- **No Node in the runtime image.** It would balloon the image.

### `nginx.conf`

```nginx
server {
  listen 80;
  server_name _;
  root /usr/share/nginx/html;

  gzip on;
  gzip_types text/css application/javascript application/json image/svg+xml;
  gzip_min_length 1024;

  # Long-cache hashed assets
  location ~* \.(js|css|woff2|png|jpg|svg|ico)$ {
    add_header Cache-Control "public, max-age=31536000, immutable";
    try_files $uri =404;
  }

  # SPA fallback
  location / {
    add_header Cache-Control "no-store";
    try_files $uri $uri/ /index.html;
  }

  # Security headers (mirror what's set globally if you have a global config)
  add_header X-Content-Type-Options "nosniff" always;
  add_header X-Frame-Options "DENY" always;
  add_header Referrer-Policy "strict-origin-when-cross-origin" always;
}
```

---

## 3. `docker-compose.yml` (Local Dev)

```yaml
services:
  postgres:
    image: postgres:16-alpine
    environment:
      POSTGRES_USER: <project>
      POSTGRES_PASSWORD: <project>_dev
      POSTGRES_DB: <project>_dev
    ports:
      - "5432:5432"
    volumes:
      - postgres_data:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U <project>"]
      interval: 10s
      timeout: 3s
      retries: 5

  redis:
    image: redis:7-alpine
    ports:
      - "6379:6379"
    volumes:
      - redis_data:/data
    healthcheck:
      test: ["CMD", "redis-cli", "ping"]
      interval: 10s
      timeout: 3s
      retries: 5

  api:
    build:
      context: ./<Project>.Core
      dockerfile: Dockerfile
    environment:
      ASPNETCORE_ENVIRONMENT: Development
      ConnectionStrings__Default: "Host=postgres;Database=<project>_dev;Username=<project>;Password=<project>_dev"
      ConnectionStrings__Redis: "redis:6379"
      Jwt__Issuer: "<Project>"
      Jwt__Audience: "<Project>.Web"
      Jwt__SigningKey: "${JWT_SIGNING_KEY:?must be set in .env or shell}"
    ports:
      - "5000:8080"
    depends_on:
      postgres:
        condition: service_healthy
      redis:
        condition: service_healthy

  web:
    build:
      context: ./<Project>.Client
      dockerfile: Dockerfile
      args:
        VITE_API_BASE_URL: "http://localhost:5000"
    ports:
      - "5173:80"
    depends_on:
      - api

volumes:
  postgres_data:
  redis_data:
```

### Rules

- **Healthchecks for every service.**
- **`depends_on: condition: service_healthy`** — `api` waits until Postgres is ready, not just running.
- **Volumes for data** so DB doesn't reset on `docker compose down`.
- **`${JWT_SIGNING_KEY:?...}`** forces the developer to set it (in `.env` or shell). The compose fails fast if missing.

---

## 4. `.env` for Compose

```bash
# .env (gitignored)
JWT_SIGNING_KEY=local-development-32-char-key-here
```

A `.env.example` (committed) lists required keys without values.

---

## 5. Local Workflow

```bash
# Start everything
docker compose up -d

# Run migrations from host
dotnet ef database update \
  --project src/<Project>.Infrastructure \
  --startup-project src/<Project>.Api \
  --connection "Host=localhost;Database=<project>_dev;Username=<project>;Password=<project>_dev"

# View logs
docker compose logs -f api

# Reset DB (destructive)
docker compose down -v
docker compose up -d postgres redis
```

### Rules

- **Migrations run from the host** during dev for fast iteration. In CI/CD they run as a job.
- **`-v` flag wipes volumes.** Use intentionally.

---

## 6. Image Tagging

| Tag                          | When to use                                      |
|------------------------------|--------------------------------------------------|
| `<image>:latest`             | Local dev only. Don't deploy `latest` to prod.   |
| `<image>:<git-sha>`          | Every CI build                                   |
| `<image>:v<MAJOR.MINOR.PATCH>` | Release builds                                |
| `<image>:<env>-<sha>`        | Env-specific builds (e.g. `staging-a1b2c3`)      |

### Rules

- **Production deploys reference the SHA**, not `latest`. Reproducibility.
- **Promote between environments** by re-tagging the same SHA — never rebuild.

---

## 7. Image Size Hygiene

Backend image budget: **< 200 MB**. Frontend image budget: **< 50 MB**.

Tactics:
- **Alpine base images** (with eyes open — glibc compatibility).
- **`UseAppHost=false`** for .NET.
- **Multi-stage build** — build artifacts only in runtime image.
- **`.dockerignore`** excludes `bin/`, `obj/`, `node_modules/`, `tests/`, `.git/`.

```dockerignore
**/bin
**/obj
**/node_modules
.git
.github
.vs
.vscode
**/.idea
**/Dockerfile*
docker-compose*.yml
README.md
```

---

## 8. Image Scanning

CI runs `trivy` (or `grype`) on every built image:

```yaml
- name: Scan image
  uses: aquasecurity/trivy-action@master
  with:
    image-ref: ghcr.io/${{ github.repository }}/api:${{ github.sha }}
    format: sarif
    severity: CRITICAL,HIGH
    exit-code: 1
```

### Rules

- **Critical / High vulnerabilities fail the build.**
- **Update base images** when the underlying distro publishes patches (Renovate handles this).

---

## 9. Production Compose vs Kubernetes

For small projects, `docker-compose.prod.yml` on a single VPS is enough:

```yaml
services:
  api:
    image: ghcr.io/<org>/<project>-api:${TAG}
    restart: unless-stopped
    environment: ...
  web:
    image: ghcr.io/<org>/<project>-web:${TAG}
    restart: unless-stopped
  postgres:
    image: postgres:16
    restart: unless-stopped
    volumes:
      - postgres_data:/var/lib/postgresql/data
  redis:
    image: redis:7
    restart: unless-stopped
  nginx:
    image: nginx:1.27
    restart: unless-stopped
    ports: ["80:80", "443:443"]
    volumes: ...
```

For larger projects: Kubernetes (AKS / EKS / GKE) or PaaS (Azure Container Apps, Cloud Run, Render).

### Rules

- **`restart: unless-stopped`** in production.
- **No `latest` tags in production.** Use the SHA / version.
- **Compose isn't an orchestrator.** Beyond a single host, switch to Kubernetes.

---

## 10. Common Mistakes

| Mistake                                                  | Fix                                                                |
|----------------------------------------------------------|--------------------------------------------------------------------|
| Single-stage Dockerfile (SDK in runtime)                 | Multi-stage; runtime image is `aspnet`                             |
| Running as root in container                             | Add a non-root user                                                |
| No `HEALTHCHECK`                                         | Always; orchestrators rely on it                                   |
| Bundle env vars baked into the image build for one env   | Build per env, or read at runtime via a runtime config endpoint    |
| Volumes missing for DB                                   | Data lost on `down`                                                |
| `docker-compose.yml` references `latest` images          | Pin SHA/version                                                    |
| `.dockerignore` missing — copies node_modules            | Always include                                                     |
| Production deploys `latest`                              | Deploy by SHA                                                      |
| Image size > 1 GB                                        | Multi-stage + Alpine + UseAppHost=false                            |
| No image scanning                                        | Trivy in CI, fail on Critical/High                                 |
