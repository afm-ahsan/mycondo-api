# Frontend Coding Standards (TypeScript / React)

Day-to-day code-quality rules. Enforced by ESLint, TypeScript strict mode, and CI.

---

## 1. TypeScript Discipline

### `tsconfig` (non-negotiable)

```jsonc
{
  "strict": true,
  "noUncheckedIndexedAccess": true,
  "noImplicitOverride": true,
  "noFallthroughCasesInSwitch": true,
  "exactOptionalPropertyTypes": true,
  "verbatimModuleSyntax": true,
  "isolatedModules": true
}
```

### Rules

- **`any` is forbidden.** Use `unknown` and narrow. ESLint: `@typescript-eslint/no-explicit-any: error`.
- **Type assertions (`as`) require justification.** Either use a type guard, a runtime parser (Zod), or a `// SAFETY: ...` comment with reasoning.
- **Prefer `type` over `interface`** for object shapes. Use `interface` only for class implementables or declaration merging.
- **No `enum`s.** Use `as const` objects or string-literal unions:
  ```ts
  type Status = 'Draft' | 'Active' | 'Closed';
  const Status = { Draft: 'Draft', Active: 'Active', Closed: 'Closed' } as const;
  type StatusValue = typeof Status[keyof typeof Status];
  ```
- **Branded types** for domain IDs to prevent mixing them up:
  ```ts
  type CustomerId = string & { readonly __brand: 'CustomerId' };
  ```
- **Discriminated unions** for state machines and remote-data shapes:
  ```ts
  type RemoteData<T, E = ApiError> =
    | { status: 'idle' }
    | { status: 'loading' }
    | { status: 'error'; error: E }
    | { status: 'success'; data: T };
  ```
- **Module boundaries have explicit return types.** Internal helpers can rely on inference.

---

## 2. Component Authoring

### File structure

```tsx
// src/modules/customers/components/CustomerList.tsx
import { useState } from 'react';

import { Button } from '@/components/ui/button';
import { DataTable } from '@/components/data/data-table';
import type { Customer } from '../api/customers.types';

import { CustomerStatusBadge } from './CustomerStatusBadge';

type CustomerListProps = Readonly<{
  customers: readonly Customer[];
  selectedId: string | null;
  onSelect: (customer: Customer) => void;
  onCreateClick: () => void;
}>;

export function CustomerList({
  customers,
  selectedId,
  onSelect,
  onCreateClick,
}: CustomerListProps) {
  // ...
}
```

### Rules

- **Function components only.** Class components are forbidden.
- **Named exports only.** No `export default` except in module routers (and route files when the framework requires it).
- **Props are `Readonly<...>`.** Use `readonly T[]` over `T[]` for incoming arrays.
- **Destructure props in the parameter list**, not in the body.
- **Component bodies stay small** — extract subcomponents or hooks beyond ~150 lines.
- **One component per file.** Sibling helper functions are fine; sibling components are not.
- **Booleans as bare props**: `<Button disabled>` not `<Button disabled={true} />`.
- **Components with 5+ props**: introduce a single `props` object only if those props are deeply related.

---

## 3. React 18 Patterns

We're on React 18.3 (matches Metronic). The React Compiler is **not** in play; manual memoization is occasionally necessary, but use it judiciously.

### Hooks

- **Custom hooks start with `use`.** ESLint enforces this.
- **One hook per file.** File name matches the hook (`useCustomerActions.ts`).
- **Hooks return either a single value, a tuple, or an object** — pick one shape and stick to it.

### When to memoize

- **`useMemo` for expensive derived values** (chart datasets, large filtered arrays).
- **`useCallback` for callbacks passed to memoized children** that would re-render on identity change.
- **`React.memo` only after profiling shows a re-render bottleneck.**

Don't reach for memoization defensively. Most components don't need it.

### `useEffect` discipline

- **`useEffect` only for synchronizing with external systems** (subscriptions, browser APIs, non-React libraries).
- **Side effects in event handlers**, not in effects:
  ```tsx
  // Good
  function handleClick() { trackEvent('clicked'); /* update state */ }

  // Bad
  useEffect(() => { trackEvent('opened'); }, []);  // For "on mount" tracking, this is fine.
  // But:
  useEffect(() => {
    if (someStateJustChanged) trackEvent('updated');  // event handler instead.
  }, [someState]);
  ```
- **No `useEffect` for derived state.** Compute in render:
  ```tsx
  // Bad: derived state in effect
  const [fullName, setFullName] = useState('');
  useEffect(() => setFullName(`${first} ${last}`), [first, last]);

  // Good: just compute it
  const fullName = `${first} ${last}`;
  ```
- **Cleanup functions for subscriptions** — every event listener registered must be removed.

---

## 4. Naming Conventions

| Kind                | Convention                  | Example                              |
|---------------------|-----------------------------|--------------------------------------|
| Component           | `PascalCase`                | `CustomerForm`                       |
| Hook                | `useCamelCase`              | `useCustomerActions`                 |
| Function            | `camelCase`, verb-first     | `formatCurrency`, `parseAmount`      |
| Boolean variable    | `is/has/should/can` prefix  | `isLoading`, `hasError`              |
| Event handler prop  | `on<Event>`                 | `onSelect`, `onChange`               |
| Event handler impl  | `handle<Event>`             | `handleSelect`, `handleSubmit`       |
| Constant            | `SCREAMING_SNAKE_CASE`      | `DEFAULT_PAGE_SIZE`                  |
| Type / Interface    | `PascalCase`, no `I` prefix | `Customer`, `CustomerFormProps`      |
| Generic param       | Single capital, descriptive | `T`, `TError`, `TData`               |
| File (component)    | `PascalCase.tsx`            | `CustomerForm.tsx`                   |
| File (utility)      | `camelCase.ts`              | `formatCurrency.ts`                  |

---

## 5. Imports

**Order** (enforced via `eslint-plugin-import`):

1. Node built-ins (rare in frontend).
2. External packages (`react`, `zod`, ...).
3. Internal aliases (`@/components/...`, `@/lib/...`).
4. Relative imports (`./types`, `../api/...`).
5. Type-only imports (separate group when using `import type`).
6. CSS / asset imports last.

```tsx
import { useState, useMemo } from 'react';
import { z } from 'zod';

import { Button } from '@/components/ui/button';
import { useAppSelector } from '@/store/hooks';
import { cn } from '@/lib/cn';

import { CustomerForm } from './CustomerForm';

import type { Customer } from '../api/customers.types';

import './CustomerList.css';
```

### Rules

- **Always use `import type`** for types — `verbatimModuleSyntax: true` enforces this.
- **No barrel files inside modules** except the top-level `index.ts`.
- **No `import * as X`** unless aliasing is genuinely clearer (rare).
- **Avoid relative imports that climb more than one `../`.** Use `@/` instead.

---

## 6. Error Handling

### `ApiError` Type

```ts
// src/api/api-error.ts
export type ApiError =
  | { kind: 'network' }
  | { kind: 'validation'; fields: Record<string, string[]> }
  | { kind: 'unauthorized' }
  | { kind: 'forbidden'; message: string }
  | { kind: 'notFound'; message: string }
  | { kind: 'conflict'; message: string }
  | { kind: 'rateLimited'; retryAfter?: number }
  | { kind: 'server'; traceId?: string };

export function isApiError(err: unknown): err is ApiError {
  return typeof err === 'object' && err !== null && 'kind' in err;
}

export function isValidationError(err: unknown): err is Extract<ApiError, { kind: 'validation' }> {
  return isApiError(err) && err.kind === 'validation';
}

export function toUserMessage(err: ApiError): string {
  switch (err.kind) {
    case 'network':       return 'Network problem. Check your connection and try again.';
    case 'validation':    return 'Some fields are invalid. Please review and try again.';
    case 'unauthorized':  return 'Please sign in to continue.';
    case 'forbidden':     return err.message || "You don't have permission for this action.";
    case 'notFound':      return err.message || 'Resource not found.';
    case 'conflict':      return err.message;
    case 'rateLimited':   return `Too many requests. ${err.retryAfter ? `Try again in ${err.retryAfter}s.` : ''}`;
    case 'server':        return 'Something went wrong on our end. Please try again later.';
  }
}
```

### `baseQuery` parses to `ApiError`

In `src/api/base-query.ts` extend `transformErrorResponse` (or wrap the baseQuery) to map ProblemDetails JSON into `ApiError` consistently. Components then type errors as `ApiError`, never `unknown`.

### Error boundaries

Every route segment has an error boundary:

```tsx
// src/components/feedback/error-boundary.tsx
import { Component, type ReactNode } from 'react';
import { ErrorState } from './error-state';

type Props = Readonly<{ children: ReactNode; fallback?: ReactNode }>;
type State = { error: Error | null };

export class ErrorBoundary extends Component<Props, State> {
  state: State = { error: null };
  static getDerivedStateFromError(error: Error) { return { error }; }
  componentDidCatch(error: Error, info: React.ErrorInfo) {
    // Log to observability
    console.error('Error boundary caught', error, info);
  }
  render() {
    if (this.state.error) return this.props.fallback ?? <ErrorState error={this.state.error} />;
    return this.props.children;
  }
}
```

### Rules

- **Every route has an `ErrorBoundary`** (typically wrapping the page in `AppShell`).
- **Show user-friendly messages**, never raw API errors.
- **Log raw errors with the correlation ID** to the observability backend.

---

## 7. Performance

- **Code-split at the route level.** Module routers are lazy-loaded.
- **Use `useDeferredValue`** for non-urgent updates (search-as-you-type).
- **Virtualize long lists** with `@tanstack/react-virtual` (anything over ~50 rows).
- **Image rules**: explicit `width`/`height`; `loading="lazy"` for off-screen; modern formats (WebP/AVIF) via `<picture>`.
- **Bundle budgets**: each route chunk < 250 KB gzipped; main bundle < 150 KB gzipped. CI fails on regressions.
- **Avoid inline arrow components** in JSX (`{() => <Foo />}`). Hoist to a named component.

---

## 8. Logging

- **No `console.log` in committed code.** Use `@/lib/logger`:
  ```ts
  // src/lib/logger.ts
  type LogLevel = 'debug' | 'info' | 'warn' | 'error';

  function log(level: LogLevel, message: string, meta?: Record<string, unknown>) {
    if (import.meta.env.PROD && level === 'debug') return;
    const ts = new Date().toISOString();
    // eslint-disable-next-line no-console
    console[level === 'debug' ? 'log' : level](`[${ts}] ${message}`, meta ?? '');
  }

  export const logger = {
    debug: (m: string, meta?: Record<string, unknown>) => log('debug', m, meta),
    info:  (m: string, meta?: Record<string, unknown>) => log('info', m, meta),
    warn:  (m: string, meta?: Record<string, unknown>) => log('warn', m, meta),
    error: (m: string, meta?: Record<string, unknown>) => log('error', m, meta),
  };
  ```
- **Errors and rare warnings ship to the observability backend** (Sentry / LogRocket / a custom endpoint). Don't log every user click.

---

## 9. Comments and Documentation

- **Comments explain *why*, not *what*.**
- **Component-level JSDoc** for non-obvious public components in `components/ui/`.
- **TODO comments include a ticket reference**: `// TODO(<TICKET-ID>): <description>`.
- **No commented-out code** in commits.

---

## 10. Forbidden Patterns

| Pattern                                  | Use instead                              |
|------------------------------------------|------------------------------------------|
| `any`                                    | `unknown` + narrowing                    |
| Class components                         | Function components + hooks              |
| `useEffect` for derived state            | Direct computation in render             |
| `localStorage` for tokens                | HTTP-only cookies (refresh) + memory (access) |
| `dangerouslySetInnerHTML`                | Render text + components (or DOMPurify if absolutely needed) |
| Inline arrow components in JSX           | Hoisted named components                 |
| `index` as React `key`                   | Stable IDs                               |
| Mutating props or state                  | Return new objects/arrays                |
| Non-null assertion `!`                   | Type guards / runtime checks             |
| `console.log` in committed code          | `@/lib/logger`                           |
| Default export (non-router)              | Named export                             |
| `enum`                                   | `as const` objects or string unions      |
| `interface` for plain object shapes      | `type`                                   |
| Mixing `npm` and `pnpm` lockfiles        | One package manager per project          |

---

## 11. Pull Request Checklist

Before merging:

- [ ] TypeScript: zero errors, zero `any`, zero unused exports.
- [ ] ESLint: zero warnings.
- [ ] Tests: new logic has unit tests; user-facing flows have an E2E test.
- [ ] Bundle size: no regression > 5%.
- [ ] a11y: keyboard-tested, axe-core clean.
- [ ] No secrets, tokens, or PII in logs or in client code.
- [ ] If the PR adds an API call: types are generated from OpenAPI, not hand-written.
- [ ] Loading and error states render correctly.
- [ ] Forms surface server-side validation errors per field.
