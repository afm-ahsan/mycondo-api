# State Management and Data Fetching

The frontend has **four kinds of state**, each with one canonical home:

| State type                      | Home                                | Why                                              |
|---------------------------------|-------------------------------------|--------------------------------------------------|
| Server data                     | **RTK Query**                       | Caching, invalidation, refetching, polling       |
| URL state (filters, paging)     | **`useSearchParams`** (React Router)| Sharable, browser-back-friendly                  |
| Form state                      | **React Hook Form** + Zod           | Performance, schema validation                   |
| Component-local UI state        | **`useState` / `useReducer`**       | Simplicity                                       |
| Cross-component client state    | **Redux slice**                     | Auth, theme, sidebar collapse                    |
| Refs / DOM access               | **`useRef`**                        | Imperative escape hatch                          |

**Mirroring server data into Redux slices or `useState` is forbidden.** It creates two sources of truth.

---

## 1. RTK Query — Shared API

There is **one** `createApi` instance for the whole app, in `src/api/base-query.ts`. Modules attach their endpoints via `api.injectEndpoints(...)`.

```ts
// src/api/base-query.ts
import { createApi, fetchBaseQuery, type BaseQueryFn } from '@reduxjs/toolkit/query/react';
import type { FetchBaseQueryError, FetchArgs } from '@reduxjs/toolkit/query';
import { env } from '@/lib/env';
import { logout, refreshToken } from '@/store/slices/auth.slice';
import type { RootState } from '@/store/store';

const rawBaseQuery = fetchBaseQuery({
  baseUrl: env.VITE_API_BASE_URL,
  prepareHeaders: (headers, { getState }) => {
    const token = (getState() as RootState).auth.accessToken;
    if (token) headers.set('Authorization', `Bearer ${token}`);
    headers.set('X-Correlation-Id', crypto.randomUUID());
    return headers;
  },
  credentials: 'include',
});

const baseQueryWithRefresh: BaseQueryFn<string | FetchArgs, unknown, FetchBaseQueryError> =
  async (args, api, extraOptions) => {
    let result = await rawBaseQuery(args, api, extraOptions);

    if (result.error?.status === 401) {
      const refreshResult = await rawBaseQuery({ url: '/api/auth/refresh', method: 'POST' }, api, extraOptions);
      if (refreshResult.data) {
        api.dispatch(refreshToken(refreshResult.data));
        result = await rawBaseQuery(args, api, extraOptions);
      } else {
        api.dispatch(logout());
      }
    }

    return result;
  };

export const api = createApi({
  reducerPath: 'api',
  baseQuery: baseQueryWithRefresh,
  tagTypes: ['Customer', 'Quotation', 'SalesOrder', 'Invoice', 'Product', /* ... */],
  endpoints: () => ({}),
});
```

### Rules

- **One `createApi` per app.** Modules use `injectEndpoints`.
- **`tagTypes`** declared centrally. Each aggregate is a tag.
- **`baseQuery` handles auth refresh** transparently — components don't see 401s for expired tokens.
- **Correlation ID** generated per request; the server echoes it back in `X-Correlation-Id`.
- **`credentials: 'include'`** for cookie-based refresh tokens.

---

## 2. Module Endpoints

Each module's RTK Query slice attaches to the shared `api`. See `02-frontend/02-module-architecture.md` §4 for the full pattern. Quick template:

```ts
// src/modules/<feature>/api/<feature>.api.ts
import { api } from '@/api/base-query';

export const featureApi = api.injectEndpoints({
  endpoints: (build) => ({
    searchFoo: build.query<PagedResult<Foo>, SearchFooQuery>({
      query: (q) => ({ url: '/api/foo', params: q }),
      providesTags: (result) =>
        result
          ? [...result.items.map(f => ({ type: 'Foo' as const, id: f.id })),
             { type: 'Foo', id: 'LIST' }]
          : [{ type: 'Foo', id: 'LIST' }],
    }),
    createFoo: build.mutation<Foo, CreateFooRequest>({
      query: (body) => ({ url: '/api/foo', method: 'POST', body }),
      invalidatesTags: [{ type: 'Foo', id: 'LIST' }],
    }),
  }),
});
```

### Rules

- **Hooks named** `use<Verb><Resource>Query` / `use<Verb><Resource>Mutation`.
- **`providesTags` for queries**, `invalidatesTags` for mutations.
- **List + item tags pattern**: each item has its own tag id; the list has tag id `'LIST'`.
- **Polling** via `pollingInterval` for live data (rare):
  ```ts
  useGetDashboardQuery(undefined, { pollingInterval: 30_000 });
  ```

---

## 3. Cache Tags Strategy

Tags drive cache invalidation. The pattern:

| Operation                                  | `providesTags` / `invalidatesTags` |
|--------------------------------------------|------------------------------------|
| `searchCustomers`                          | provides `[{ Customer, id }, ..., { Customer, LIST }]` |
| `getCustomerById(id)`                      | provides `[{ Customer, id }]`      |
| `createCustomer`                           | invalidates `[{ Customer, LIST }]` |
| `updateCustomer({ id, ... })`              | invalidates `[{ Customer, id }, { Customer, LIST }]` |
| `deleteCustomer(id)`                       | invalidates `[{ Customer, id }, { Customer, LIST }]` |

### Cross-aggregate invalidation

When creating an invoice updates customer outstanding balance:

```ts
createInvoice: build.mutation<Invoice, CreateInvoiceRequest>({
  query: (body) => ({ url: '/api/invoices', method: 'POST', body }),
  invalidatesTags: (_, __, body) => [
    { type: 'Invoice', id: 'LIST' },
    { type: 'Customer', id: body.customerId },   // refresh customer detail
    { type: 'Dashboard', id: 'OUTSTANDING' },    // refresh dashboard widget
  ],
}),
```

### Rules

- **Be conservative with `'LIST'` invalidation.** It refetches every paginated page; consider granular invalidation for large lists.
- **Use the `Dashboard` tag for cross-cutting widgets.**

---

## 4. Optimistic Updates (Sparingly)

For high-frequency UI actions where waiting for the server feels laggy:

```ts
toggleFavorite: build.mutation<void, { id: string; isFavorite: boolean }>({
  query: ({ id, isFavorite }) => ({
    url: `/api/customers/${id}/favorite`,
    method: 'PUT',
    body: { isFavorite },
  }),
  async onQueryStarted({ id, isFavorite }, { dispatch, queryFulfilled }) {
    const patch = dispatch(
      featureApi.util.updateQueryData('getCustomerById', id, (draft) => {
        draft.isFavorite = isFavorite;
      })
    );
    try {
      await queryFulfilled;
    } catch {
      patch.undo();
    }
  },
}),
```

### Rules

- **Optimistic updates only for low-stakes actions.** Don't use for money-changing operations.
- **Always undo on error.** A failed mutation must roll back the cache.
- **For most cases, `invalidatesTags` is enough.** Don't pre-optimize.

---

## 5. Search Params for URL State

List filters, pagination, sort live in the URL — not in component state:

```tsx
import { useSearchParams } from 'react-router-dom';
import { z } from 'zod';

const searchSchema = z.object({
  search: z.string().optional(),
  page: z.coerce.number().int().positive().default(1),
  pageSize: z.coerce.number().int().positive().max(100).default(20),
  sort: z.enum(['name', 'createdAt']).default('createdAt'),
});

export function useCustomersUrlState() {
  const [params, setParams] = useSearchParams();
  const parsed = searchSchema.parse(Object.fromEntries(params));

  function update(next: Partial<z.infer<typeof searchSchema>>) {
    const merged = { ...parsed, ...next };
    setParams(new URLSearchParams(
      Object.entries(merged)
        .filter(([, v]) => v !== undefined && v !== '')
        .map(([k, v]) => [k, String(v)])
    ));
  }

  return { ...parsed, update };
}
```

### Rules

- **All list-page filters in the URL.** Sharable, refresh-safe, browser-back-friendly.
- **Validate URL params with Zod.** Garbage in URL = parse error → fall back to defaults.
- **Bookmark-friendly URLs** like `/customers?search=alice&page=2&sort=name`.

---

## 6. Redux Slices (Cross-Cutting Client State Only)

Redux slices live in `src/store/slices/` and hold **only**:

- **Auth state** (current user, access token).
- **UI state** (theme, sidebar collapsed, current locale).
- **Notifications/toasts queue** (if not handled by a library like sonner).

```ts
// src/store/slices/auth.slice.ts
import { createSlice, type PayloadAction } from '@reduxjs/toolkit';
import type { AuthUser } from '@/types/auth.types';

type AuthState = {
  user: AuthUser | null;
  accessToken: string | null;
};

const initialState: AuthState = { user: null, accessToken: null };

export const authSlice = createSlice({
  name: 'auth',
  initialState,
  reducers: {
    loginSucceeded(state, action: PayloadAction<{ user: AuthUser; accessToken: string }>) {
      state.user = action.payload.user;
      state.accessToken = action.payload.accessToken;
    },
    refreshToken(state, action: PayloadAction<{ accessToken: string }>) {
      state.accessToken = action.payload.accessToken;
    },
    logout(state) {
      state.user = null;
      state.accessToken = null;
    },
  },
});

export const { loginSucceeded, refreshToken, logout } = authSlice.actions;
export const authReducer = authSlice.reducer;
```

### Rules

- **Server data NEVER goes into a slice.** Customer list = RTK Query, not slice.
- **One slice per concern.** Don't write a god-slice for "everything UI".
- **Selectors typed** via `useAppSelector`.
- **`createSlice` only.** No raw `createReducer` or hand-written reducers.

---

## 7. When to Use What — Decision Tree

```
Does the data come from the server?
  YES → RTK Query
  NO → continue ↓

Should the data survive a page reload / be sharable via URL?
  YES → URL via useSearchParams
  NO → continue ↓

Is it form state being edited?
  YES → React Hook Form
  NO → continue ↓

Is it shared across many unrelated components (auth, theme)?
  YES → Redux slice
  NO → useState / useReducer
```

---

## 8. Loading and Error States

Pages handle three states explicitly:

```tsx
const { data, isLoading, error } = useGetCustomerByIdQuery(id);

if (isLoading) return <ContentLoader />;
if (error) return <ErrorState error={error} />;
if (!data) return <EmptyState message="Customer not found" />;

return <CustomerDetail customer={data} />;
```

### Rules

- **`ContentLoader` is the standard skeleton.** Use page-level skeletons over centered spinners.
- **`ErrorState`** parses the typed `ApiError` and renders an actionable message (with retry button if applicable).
- **`EmptyState`** is a designed component, not just `<p>No data</p>`.
- **Detail in** `02-frontend/06-coding-standards.md` for the full error type.

---

## 9. Polling and Real-Time

For dashboards or near-real-time data:

```tsx
const { data } = useGetDashboardQuery(undefined, {
  pollingInterval: 30_000,        // 30s
  refetchOnFocus: true,           // refetch when tab refocuses
  refetchOnReconnect: true,       // refetch on network recovery
});
```

For true real-time (chat, live counters), consider WebSockets via a custom RTK Query streaming endpoint. Use sparingly.

---

## 10. Common Mistakes

| Mistake                                                         | Fix                                                                |
|-----------------------------------------------------------------|--------------------------------------------------------------------|
| Storing fetched customer list in `useState`                     | RTK Query                                                          |
| Storing filters in component state then losing them on reload   | URL via `useSearchParams`                                          |
| Multiple `createApi` instances                                  | One shared `api` + `injectEndpoints`                               |
| Manually refetching after mutation via `refetch()`              | `invalidatesTags`                                                  |
| Mirroring RTK Query data into a slice "for convenience"         | Use `useGetXyzQuery(...)` directly                                 |
| `pollingInterval: 1000` for non-critical data                   | 30 seconds is usually fine; or use `refetchOnFocus`                |
| Optimistic update without `patch.undo()` on error               | Always undo on failure                                             |
| Untyped `useDispatch()` / `useSelector()`                       | `useAppDispatch` / `useAppSelector` from `store/hooks.ts`          |
| `tagTypes: []` (forgetting tags entirely)                       | Declare every aggregate up-front                                   |
