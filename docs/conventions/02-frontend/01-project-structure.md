# Frontend Project Structure

**Stack**: React 18.3 · TypeScript 5.6+ (strict) · Vite · Metronic React Vite Template · Redux Toolkit + RTK Query · React Hook Form + Zod · Tailwind CSS v4 · Vitest · Playwright

The frontend is built **on top of the Metronic React Vite template**. We adapt it — we don't replace it. The auth scaffolding, layouts, theme, and component library are kept; business modules are added under `src/modules/`.

> Replace `<feature>` with the kebab-case feature name (`customers`, `quotations`, `sales-orders`).

---

## 1. Top-Level Layout

```
<project>-web/
├── .editorconfig
├── .gitignore
├── .nvmrc                            # Node version pin (Node 22 LTS)
├── .env                              # Local values (gitignored)
├── .env.example                      # Documents required vars (committed)
├── .prettierrc / .prettierignore
├── eslint.config.ts                  # Flat config
├── tsconfig.json
├── tsconfig.app.json
├── tsconfig.node.json
├── vite.config.ts
├── vitest.config.ts
├── playwright.config.ts
├── tailwind.config.ts                # Mostly thin; tokens via CSS @theme
├── postcss.config.mjs
├── package.json
├── package-lock.json (or pnpm-lock.yaml)
├── components.json                   # shadcn/ui config
├── index.html
├── Dockerfile
├── nginx.conf                        # For static-served production build
├── README.md
├── CLAUDE.md
│
├── public/                           # Static assets (favicon, robots.txt, /media/)
│
├── e2e/                              # Playwright tests (top-level)
│   ├── fixtures/
│   └── tests/
│       └── <feature>.spec.ts
│
└── src/
    ├── main.tsx                      # Entry — providers wired here
    ├── App.tsx                       # Root: providers, router
    │
    ├── auth/                         # Metronic auth (kept; adapted to our backend)
    │   ├── auth-routing.tsx
    │   ├── require-auth.tsx
    │   ├── layouts/
    │   ├── pages/
    │   └── providers/
    │
    ├── components/                   # Cross-feature shared UI
    │   ├── ui/                       # shadcn-style primitives (from Metronic)
    │   ├── layout/                   # AppShell, Sidebar, Topbar
    │   ├── feedback/                 # ErrorBoundary, EmptyState, LoadingSpinner
    │   ├── data/                     # DataTable, Pagination
    │   └── common/                   # Container, Icons, ContentLoader
    │
    ├── modules/                      # Business features (the heart of the app)
    │   └── <feature>/
    │       ├── api/
    │       ├── components/
    │       ├── hooks/
    │       ├── pages/
    │       ├── schemas/
    │       ├── types/
    │       └── index.ts              # Public surface
    │
    ├── routes/                       # Route configuration (lazy-loaded modules)
    │   └── app-routes.tsx
    │
    ├── store/                        # Redux store + middleware setup
    │   ├── store.ts
    │   ├── hooks.ts                  # Typed useAppDispatch / useAppSelector
    │   └── slices/                   # Cross-cutting client-state slices (rare)
    │
    ├── api/                          # Shared baseQuery, error handling, interceptors
    │   ├── base-query.ts
    │   ├── api-error.ts
    │   ├── interceptors.ts
    │   └── api-tags.ts
    │
    ├── lib/                          # Pure utilities (no React)
    │   ├── env.ts                    # Validated env via Zod
    │   ├── config.ts
    │   ├── constants.ts
    │   ├── date.ts
    │   ├── format.ts
    │   ├── cn.ts                     # clsx + tailwind-merge
    │   └── logger.ts
    │
    ├── hooks/                        # Cross-feature hooks (rare)
    │
    ├── i18n/                         # If localized
    │
    ├── styles/                       # Global CSS, Tailwind layers, Metronic theme
    │   ├── globals.css               # @import "tailwindcss"
    │   └── metronic/                 # Metronic CSS modules (kept as-is)
    │
    ├── types/                        # Cross-cutting TS types
    │   └── api.types.ts              # Shared types (Pagination, ApiError, etc.)
    │
    ├── config/                       # App configuration (menu, routes constants)
    │   ├── menu.config.ts
    │   ├── routes.constants.ts
    │   └── settings.ts
    │
    └── test/                         # Test setup (Vitest)
        ├── setup.ts
        └── msw/
            └── handlers.ts
```

---

## 2. Package Manager and Node

- **Node** pinned via `.nvmrc` to **22 LTS**.
- **Package manager** — match what Metronic ships with the project (typically **npm**). pnpm acceptable per project but don't mix.
- **Lockfile committed.** No `--no-frozen-lockfile` in CI.

```jsonc
// package.json scripts (canonical)
{
  "scripts": {
    "dev": "vite",
    "build": "tsc -b && vite build",
    "preview": "vite preview",
    "test": "vitest run",
    "test:watch": "vitest",
    "test:e2e": "playwright test",
    "lint": "eslint .",
    "lint:fix": "eslint . --fix",
    "format": "prettier --write .",
    "typecheck": "tsc -b --noEmit"
  }
}
```

---

## 3. TypeScript Configuration

`tsconfig.app.json` (the file Vite reads):

```jsonc
{
  "extends": "./tsconfig.json",
  "compilerOptions": {
    "target": "ES2022",
    "module": "ESNext",
    "moduleResolution": "Bundler",
    "jsx": "react-jsx",
    "strict": true,
    "noUncheckedIndexedAccess": true,
    "noImplicitOverride": true,
    "noFallthroughCasesInSwitch": true,
    "exactOptionalPropertyTypes": true,
    "verbatimModuleSyntax": true,
    "isolatedModules": true,
    "skipLibCheck": true,
    "esModuleInterop": true,
    "resolveJsonModule": true,
    "allowImportingTsExtensions": true,
    "noEmit": true,
    "paths": {
      "@/*": ["src/*"]
    },
    "baseUrl": "."
  },
  "include": ["src", "vite.config.ts"]
}
```

### Rules

- **`strict: true`** is non-negotiable.
- **Path alias `@/`** → `src/`. No relative imports that climb more than one `../`.
- **`verbatimModuleSyntax: true`** — `import type` for types.
- **No `any`.** ESLint enforces `@typescript-eslint/no-explicit-any: error`.

---

## 4. Vite Configuration

```ts
// vite.config.ts
import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import tailwindcss from '@tailwindcss/vite';
import path from 'node:path';

export default defineConfig({
  plugins: [react(), tailwindcss()],
  resolve: {
    alias: { '@': path.resolve(__dirname, './src') }
  },
  server: {
    port: 5173,
    strictPort: true,
    proxy: {
      '/api': {
        target: process.env.VITE_API_BASE_URL ?? 'http://localhost:5000',
        changeOrigin: true,
        secure: false
      }
    }
  },
  build: {
    sourcemap: true,
    chunkSizeWarningLimit: 600
  }
});
```

### Rules

- **Dev proxies `/api`** to the backend so the frontend never CORS-talks during dev.
- **Sourcemaps in production** — they're served from a CDN behind auth, not public.

---

## 5. Environment Variables

`.env.example` (committed):

```
VITE_API_BASE_URL=http://localhost:5000
VITE_APP_NAME=<Project>
VITE_APP_VERSION=0.1.0
VITE_ENV=development
VITE_FEATURE_REPORTS=false
```

Validated in `src/lib/env.ts`:

```ts
import { z } from 'zod';

const envSchema = z.object({
  VITE_API_BASE_URL: z.string().url(),
  VITE_APP_NAME: z.string().min(1),
  VITE_APP_VERSION: z.string().min(1),
  VITE_ENV: z.enum(['development', 'staging', 'production', 'test']),
  VITE_FEATURE_REPORTS: z.enum(['true', 'false']).transform(s => s === 'true').default('false'),
});

export const env = envSchema.parse(import.meta.env);
```

### Rules

- **Only `VITE_*` env vars are exposed** to the bundle.
- **Always validated at startup.** Bad config = startup error, not runtime crash.
- **`.env` gitignored.** `.env.example` committed.
- **No secrets in env vars** that ship to the browser. Browser env vars are public.

---

## 6. Module Folder Layout

Each module under `src/modules/<feature>/`:

```
src/modules/customers/
├── index.ts                          # Public surface
├── api/
│   ├── customers.api.ts              # RTK Query slice (createApi)
│   └── customers.types.ts            # Request/response types (or generated)
├── components/                       # Feature-private UI
│   ├── CustomerForm.tsx
│   ├── CustomerForm.test.tsx
│   ├── CustomerList.tsx
│   ├── CustomerDetail.tsx
│   └── CustomerStatusBadge.tsx
├── hooks/                            # Feature-private hooks
│   ├── useCustomerActions.ts
│   └── useCustomerFilters.ts
├── pages/                            # Page-level components rendered by routes
│   ├── CustomersListPage.tsx
│   ├── CustomerCreatePage.tsx
│   ├── CustomerEditPage.tsx
│   └── CustomerDetailPage.tsx
├── schemas/                          # Zod schemas (form + responses)
│   └── customer.schema.ts
└── types/                            # Module-private types (if not generated from OpenAPI)
    └── customer.types.ts
```

### Rules

- **`index.ts` exposes only what other modules need** — typically pages and a few hooks.
- **No cross-module imports of internals.** `modules/quotations` may only import from `modules/customers/index.ts`. ESLint `import/no-restricted-paths` enforces this.
- **If logic is needed by 2+ modules**, promote to `src/components/`, `src/hooks/`, or `src/lib/`.

---

## 7. Routes

```ts
// src/routes/app-routes.tsx
import { Routes, Route } from 'react-router-dom';
import { lazy, Suspense } from 'react';
import { ScreenLoader } from '@/components/common/screen-loader';
import { RequireAuth } from '@/auth/require-auth';
import { AppShell } from '@/components/layout/app-shell';

const CustomersRouter = lazy(() => import('@/modules/customers'));
const QuotationsRouter = lazy(() => import('@/modules/quotations'));
const Dashboard = lazy(() => import('@/modules/dashboard'));

export function AppRoutes() {
  return (
    <Suspense fallback={<ScreenLoader />}>
      <Routes>
        <Route element={<RequireAuth />}>
          <Route element={<AppShell />}>
            <Route index element={<Dashboard />} />
            <Route path="customers/*" element={<CustomersRouter />} />
            <Route path="quotations/*" element={<QuotationsRouter />} />
            {/* ... */}
          </Route>
        </Route>
      </Routes>
    </Suspense>
  );
}
```

### Rules

- **Route configuration in `src/routes/`**, not scattered across modules.
- **Each module exports its own router** (a default export in `modules/<feature>/index.ts`) — keeps routing logic close to the feature.
- **Lazy-load module routers** for code splitting.
- **`RequireAuth` wraps protected routes.** Public routes are explicit.

---

## 8. Store Setup

```ts
// src/store/store.ts
import { configureStore } from '@reduxjs/toolkit';
import { setupListeners } from '@reduxjs/toolkit/query';
import { api } from '@/api/base-query';
import { authReducer } from '@/store/slices/auth.slice';
import { uiReducer } from '@/store/slices/ui.slice';

export const store = configureStore({
  reducer: {
    [api.reducerPath]: api.reducer,
    auth: authReducer,
    ui: uiReducer,
  },
  middleware: (getDefault) => getDefault().concat(api.middleware),
  devTools: import.meta.env.DEV,
});

setupListeners(store.dispatch);

export type RootState = ReturnType<typeof store.getState>;
export type AppDispatch = typeof store.dispatch;
```

```ts
// src/store/hooks.ts
import { useDispatch, useSelector, type TypedUseSelectorHook } from 'react-redux';
import type { RootState, AppDispatch } from './store';

export const useAppDispatch: () => AppDispatch = useDispatch;
export const useAppSelector: TypedUseSelectorHook<RootState> = useSelector;
```

### Rules

- **One store per app.** No multiple stores.
- **RTK Query is always added** as `api.reducerPath` and `api.middleware`.
- **Slices for cross-cutting client state** (`auth`, `ui`). Most module state lives in RTK Query (server state) or `useState` (local state).
- **Typed hooks (`useAppDispatch`, `useAppSelector`)** used everywhere — never untyped `useDispatch`/`useSelector`.

---

## 9. Naming Rules

| Kind                          | Convention             | Example                          |
|-------------------------------|------------------------|----------------------------------|
| Component file                | `PascalCase.tsx`       | `CustomerForm.tsx`               |
| Component test                | `PascalCase.test.tsx`  | `CustomerForm.test.tsx`          |
| Hook file                     | `camelCase.ts`         | `useCustomerActions.ts`          |
| Util / lib file               | `camelCase.ts`         | `formatCurrency.ts`              |
| Type-only file                | `*.types.ts`           | `customer.types.ts`              |
| Zod schema file               | `*.schema.ts`          | `customer.schema.ts`             |
| RTK Query slice file          | `*.api.ts`             | `customers.api.ts`               |
| Redux slice file              | `*.slice.ts`           | `auth.slice.ts`                  |
| Constant file                 | `*.constants.ts`       | `routes.constants.ts`            |
| Folder                        | `kebab-case`           | `user-profile/`, `sales-orders/` |
| Feature name                  | `kebab-case`           | `customers`, `amc-contracts`     |

---

## 10. Allowed Import Direction

```
routes/   ──►  modules/<feature>  ──►  components/, hooks/, lib/, api/, store/
                    │
                    └──►  modules/<other>/index.ts   (public API only)
```

- `lib/` may not import from anything except `lib/`.
- `components/ui/` may not import from `modules/`.
- `modules/A` may not import from `modules/B/components/...` — only `modules/B`.

Enforced via **ESLint `import/no-restricted-paths`** in CI.

---

## 11. Public Folder

```
public/
├── favicon.ico
├── robots.txt
├── manifest.webmanifest                # PWA (optional)
└── media/
    ├── logos/
    └── illustrations/
```

### Rules

- **No code in `public/`** — static assets only.
- **No imported assets in `public/`** — those go in `src/assets/` and pass through Vite for hashing.

---

## 12. Common Mistakes

| Mistake                                                          | Fix                                                                  |
|------------------------------------------------------------------|----------------------------------------------------------------------|
| Putting business logic in `App.tsx`                              | Move to a module                                                     |
| Two modules importing each other's internals                     | Promote shared code to `src/lib/` or `src/components/`               |
| Mirroring server data into a Redux slice                         | RTK Query is the only home for server state                          |
| Hard-coded `http://localhost:5000` URLs                          | `env.VITE_API_BASE_URL`                                              |
| Mixing `npm` and `pnpm` lockfiles                                | One package manager per project                                      |
| `index.ts` re-exports every internal file                        | Export only the module's public surface                              |
| Long relative imports (`../../../components/...`)                | Use `@/components/...`                                               |
| Files at the root of `src/components/` mixing concerns           | Use sub-folders (`ui/`, `layout/`, `feedback/`)                      |
