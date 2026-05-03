# Frontend Testing Strategy

Tests are written **with** the code, not after. Three layers:

```
                ┌────────────┐
                │     E2E    │   ← critical user journeys (Playwright)
                └────────────┘
            ┌────────────────────┐
            │  Component / Unit  │   ← Vitest + React Testing Library
            └────────────────────┘
        ┌────────────────────────────┐
        │   Pure logic / Schemas     │   ← Vitest, no React
        └────────────────────────────┘
```

---

## 1. Vitest Setup

```ts
// vitest.config.ts
import { defineConfig } from 'vitest/config';
import react from '@vitejs/plugin-react';
import path from 'node:path';

export default defineConfig({
  plugins: [react()],
  resolve: { alias: { '@': path.resolve(__dirname, './src') } },
  test: {
    globals: true,
    environment: 'jsdom',
    setupFiles: ['./src/test/setup.ts'],
    coverage: {
      provider: 'v8',
      reporter: ['text', 'lcov', 'html'],
      exclude: ['**/*.test.*', '**/*.spec.*', '**/test/**', 'src/main.tsx', 'e2e/**'],
      thresholds: { lines: 70, functions: 70, branches: 65, statements: 70 },
    },
  },
});
```

```ts
// src/test/setup.ts
import '@testing-library/jest-dom/vitest';
import { afterEach } from 'vitest';
import { cleanup } from '@testing-library/react';
import { server } from './msw/server';

beforeAll(() => server.listen({ onUnhandledRequest: 'error' }));
afterEach(() => { cleanup(); server.resetHandlers(); });
afterAll(() => server.close());
```

### Rules

- **`globals: true`** so `describe/it/expect` are auto-imported.
- **`jsdom` environment** for component tests.
- **MSW for HTTP mocking** — same handlers in unit tests, Storybook, and dev mode.
- **Coverage thresholds** enforced in CI; PR fails if below.

---

## 2. Pure Logic Tests

For utilities and Zod schemas — no React needed:

```ts
// src/lib/format.test.ts
import { describe, it, expect } from 'vitest';
import { formatCurrency, parseAmount } from './format';

describe('formatCurrency', () => {
  it('formats USD with two decimals', () => {
    expect(formatCurrency(1234.5, 'USD')).toBe('$1,234.50');
  });

  it('handles zero', () => {
    expect(formatCurrency(0, 'USD')).toBe('$0.00');
  });
});

// src/modules/customers/schemas/customer.schema.test.ts
import { customerFormSchema } from './customer.schema';

describe('customerFormSchema', () => {
  it('accepts valid data', () => {
    const result = customerFormSchema.safeParse({
      name: 'Alice', email: 'alice@example.com', notes: '', contactPersons: [],
    });
    expect(result.success).toBe(true);
  });

  it('rejects empty name', () => {
    const result = customerFormSchema.safeParse({
      name: '', email: 'alice@example.com', notes: '', contactPersons: [],
    });
    expect(result.success).toBe(false);
  });
});
```

### Rules

- **Test schemas explicitly** — they're code, they need tests.
- **Test pure logic in isolation.** No React, no DOM.
- **Coverage target ~95%** for `lib/` and `schemas/`.

---

## 3. Component Tests (React Testing Library)

```tsx
// src/modules/customers/components/CustomerForm.test.tsx
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, it, expect, vi } from 'vitest';
import { CustomerForm } from './CustomerForm';

describe('CustomerForm', () => {
  it('renders empty form with default values', () => {
    render(<CustomerForm onSubmit={vi.fn()} />);

    expect(screen.getByLabelText(/name/i)).toHaveValue('');
    expect(screen.getByLabelText(/email/i)).toHaveValue('');
    expect(screen.getByRole('button', { name: /save/i })).toBeEnabled();
  });

  it('shows validation errors when submitting empty form', async () => {
    const onSubmit = vi.fn();
    render(<CustomerForm onSubmit={onSubmit} />);

    await userEvent.click(screen.getByRole('button', { name: /save/i }));

    expect(await screen.findByText(/name is required/i)).toBeInTheDocument();
    expect(onSubmit).not.toHaveBeenCalled();
  });

  it('submits with parsed values', async () => {
    const onSubmit = vi.fn();
    render(<CustomerForm onSubmit={onSubmit} />);

    await userEvent.type(screen.getByLabelText(/name/i), 'Alice');
    await userEvent.type(screen.getByLabelText(/email/i), 'Alice@Example.com');
    await userEvent.click(screen.getByRole('button', { name: /save/i }));

    expect(onSubmit).toHaveBeenCalledWith(expect.objectContaining({
      name: 'Alice',
      email: 'alice@example.com',   // schema lowercases
    }));
  });

  it('disables submit while in flight', () => {
    render(<CustomerForm onSubmit={vi.fn()} isSubmitting />);
    expect(screen.getByRole('button', { name: /saving/i })).toBeDisabled();
  });
});
```

### Rules

- **Accessible queries**: `getByRole`, `getByLabelText`, `getByText`. Avoid `getByTestId` unless absolutely needed.
- **`userEvent` over `fireEvent`** — simulates real interactions (focus, typing delay).
- **Test the public API**: what the user sees and what callbacks receive. Don't reach into RHF internals.
- **No snapshot tests** for non-trivial UI. They rot. Use explicit assertions.

---

## 4. Testing Components That Use the Store

Wrap components in a `TestProviders` helper:

```tsx
// src/test/test-providers.tsx
import { Provider as ReduxProvider } from 'react-redux';
import { MemoryRouter } from 'react-router-dom';
import type { ReactNode } from 'react';
import { configureStore } from '@reduxjs/toolkit';
import { api } from '@/api/base-query';
import { authReducer } from '@/store/slices/auth.slice';
import { uiReducer } from '@/store/slices/ui.slice';

export function makeStore(preloaded?: any) {
  return configureStore({
    reducer: { [api.reducerPath]: api.reducer, auth: authReducer, ui: uiReducer },
    middleware: (g) => g().concat(api.middleware),
    preloadedState: preloaded,
  });
}

type Props = Readonly<{
  children: ReactNode;
  preloadedState?: any;
  initialEntries?: string[];
}>;

export function TestProviders({ children, preloadedState, initialEntries = ['/'] }: Props) {
  return (
    <ReduxProvider store={makeStore(preloadedState)}>
      <MemoryRouter initialEntries={initialEntries}>{children}</MemoryRouter>
    </ReduxProvider>
  );
}
```

Use it in tests:

```tsx
import { render, screen } from '@testing-library/react';
import { TestProviders } from '@/test/test-providers';
import { CustomersListPage } from './CustomersListPage';

it('shows customer rows from API', async () => {
  render(
    <TestProviders>
      <CustomersListPage />
    </TestProviders>
  );

  expect(await screen.findByText(/Alice Inc/i)).toBeInTheDocument();
});
```

### Rules

- **Fresh store per test.** Don't share state across tests.
- **Preload auth state** when testing protected routes.
- **MSW handles API responses** — no need to mock RTK Query directly.

---

## 5. MSW (Mock Service Worker)

```ts
// src/test/msw/handlers.ts
import { http, HttpResponse } from 'msw';
import { env } from '@/lib/env';

const base = env.VITE_API_BASE_URL;

export const handlers = [
  http.get(`${base}/api/customers`, () =>
    HttpResponse.json({
      items: [
        { id: '1', name: 'Alice Inc', email: 'alice@example.com', status: 'Active', createdAtUtc: '2026-01-01T00:00:00Z' },
        { id: '2', name: 'Bob Ltd',   email: 'bob@example.com',   status: 'Active', createdAtUtc: '2026-01-02T00:00:00Z' },
      ],
      page: 1, pageSize: 20, total: 2,
    })
  ),

  http.post(`${base}/api/customers`, async ({ request }) => {
    const body = await request.json() as { email?: string };
    if (body.email === 'taken@example.com') {
      return HttpResponse.json(
        { type: 'https://httpstatuses.io/409', title: 'Conflict', status: 409, detail: 'Email already exists' },
        { status: 409 }
      );
    }
    return HttpResponse.json({ id: 'new', ...body }, { status: 201 });
  }),
];

// src/test/msw/server.ts
import { setupServer } from 'msw/node';
import { handlers } from './handlers';
export const server = setupServer(...handlers);
```

### Rules

- **One handler set used everywhere.** Tests, dev mode (optional), Storybook.
- **Override per test** with `server.use(...)` for edge cases.
- **`onUnhandledRequest: 'error'`** — fail the test if a request isn't mocked.

---

## 6. Hook Tests

For custom hooks:

```ts
import { renderHook, act } from '@testing-library/react';
import { useCustomerFilters } from './useCustomerFilters';

it('resets to defaults', () => {
  const { result } = renderHook(() => useCustomerFilters());

  act(() => result.current.setSearch('alice'));
  act(() => result.current.setPage(3));

  act(() => result.current.reset());

  expect(result.current.search).toBe('');
  expect(result.current.page).toBe(1);
});
```

---

## 7. End-to-End — Playwright

```ts
// e2e/tests/customers.spec.ts
import { test, expect } from '@playwright/test';

test.describe('customers', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/login');
    await page.getByLabel(/email/i).fill('admin@test.local');
    await page.getByLabel(/password/i).fill('Test1234!');
    await page.getByRole('button', { name: /sign in/i }).click();
    await expect(page).toHaveURL(/\/dashboard$/);
  });

  test('user can create a customer', async ({ page }) => {
    await page.goto('/customers');
    await page.getByRole('button', { name: /add customer/i }).click();

    await page.getByLabel(/name/i).fill('Acme Hospital');
    await page.getByLabel(/email/i).fill('contact@acme.test');
    await page.getByRole('button', { name: /save/i }).click();

    await expect(page.getByText(/customer created/i)).toBeVisible();
    await expect(page).toHaveURL(/\/customers\/[a-f0-9-]+$/);
    await expect(page.getByText('Acme Hospital')).toBeVisible();
  });
});
```

```ts
// playwright.config.ts
import { defineConfig, devices } from '@playwright/test';

export default defineConfig({
  testDir: './e2e/tests',
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  workers: process.env.CI ? 2 : undefined,
  reporter: [['html', { open: 'never' }]],
  use: {
    baseURL: process.env.E2E_BASE_URL ?? 'http://localhost:5173',
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
    video: 'retain-on-failure',
  },
  projects: [
    { name: 'chromium', use: { ...devices['Desktop Chrome'] } },
    { name: 'firefox',  use: { ...devices['Desktop Firefox'] } },
  ],
  webServer: process.env.CI ? undefined : {
    command: 'npm run dev',
    port: 5173,
    reuseExistingServer: true,
  },
});
```

### Rules

- **Cover critical user journeys per module** — login, list/create/edit/delete for the main aggregates, role-restricted actions.
- **Accessible selectors only.** `getByRole`, `getByLabel`, `getByText`. Avoid CSS selectors.
- **Tests are deterministic.** No `waitForTimeout`; only `waitFor` with assertions.
- **Trace + screenshot on failure** for fast debugging.
- **Run against a fresh DB** in CI (Docker compose with seed).

---

## 8. Visual Regression (Optional)

For pixel-stable design systems, use Playwright's screenshot comparison:

```ts
test('customer card matches design', async ({ page }) => {
  await page.goto('/customers/1');
  await expect(page.locator('[data-testid="customer-card"]')).toHaveScreenshot();
});
```

### Rules

- **Use sparingly.** Visual tests are flaky and slow.
- **Pin Chromium version** in CI to avoid rendering drift.

---

## 9. Test Data

- **Builders for complex objects** in `src/test/builders/`:
  ```ts
  export function aCustomer(overrides?: Partial<Customer>): Customer {
    return {
      id: 'c1', name: 'Default', email: 'd@e.com', status: 'Active',
      createdAtUtc: '2026-01-01T00:00:00Z', ...overrides,
    };
  }
  ```
- **Shared fixtures** in `e2e/fixtures/` for Playwright (test users, seed data).

---

## 10. CI Integration

```yaml
# .github/workflows/frontend.yml (excerpt)
- run: npm ci
- run: npm run typecheck
- run: npm run lint
- run: npm test -- --coverage
- run: npm run build
- run: npx playwright install --with-deps
- run: npm run test:e2e
- uses: actions/upload-artifact@v4
  if: failure()
  with:
    name: playwright-report
    path: playwright-report/
```

### Rules

- **Tests run on every PR.** Red CI blocks merge.
- **Playwright HTML report uploaded** on failure for debugging.
- **Coverage uploaded to Codecov** with PR comment.

---

## 11. Common Mistakes

| Mistake                                                      | Fix                                                            |
|--------------------------------------------------------------|----------------------------------------------------------------|
| Snapshot tests for UI                                        | Explicit assertions on visible content                         |
| `getByTestId` everywhere                                     | Accessible queries                                             |
| Tests calling RTK Query directly                             | MSW handles HTTP at the boundary                               |
| Sharing store state across tests                             | Fresh store per test                                           |
| `waitForTimeout(...)`                                        | `waitFor` with an assertion                                    |
| Asserting on inline styles                                   | Assert on classes or visible content                           |
| Test names like `it('works')`                                | `it('shows error when name is empty')`                         |
| Skipping E2E because "tests are flaky"                       | Fix the determinism; add explicit waits                        |
| Mocking RHF or Redux                                         | Render the real thing inside `TestProviders`                   |
| Tests that pass locally, fail in CI                          | Likely an ordering/cleanup issue; review setup hooks           |
