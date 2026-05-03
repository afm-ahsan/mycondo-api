# Module Architecture (Feature-Sliced)

The frontend is organized **by business capability**, not by technical type. A module is a self-contained slice that exposes a public surface and hides its internals.

> Replace `<feature>` with the module name in kebab-case (`customers`, `quotations`).

---

## 1. What Lives Where

| Location                                 | What it contains                                          |
|------------------------------------------|-----------------------------------------------------------|
| `src/modules/<feature>/`                 | A business module (the heart of the app)                  |
| `src/components/`                        | Cross-feature shared UI (presentational only)             |
| `src/hooks/`                             | Cross-feature shared hooks (rare; promotion target)       |
| `src/lib/`                               | Pure utilities (no React)                                 |
| `src/api/`                               | Shared baseQuery, error parsing, RTK Query setup          |
| `src/store/`                             | Redux store, middleware, cross-cutting client slices      |
| `src/auth/`                              | Authentication flow (Metronic-provided, adapted)          |
| `src/routes/`                            | Route configuration + lazy module mounting                |
| `src/types/`                             | Cross-cutting TS types (`PagedResult`, `ApiError`)        |

If you can't decide where something belongs:

1. Is it specific to one module's UI / data flow? → `modules/<feature>/`
2. Is it pure logic with no React? → `lib/`
3. Is it a generic, presentational React component? → `components/`
4. Is it cross-cutting state (auth, theme)? → `store/slices/`
5. Is it shared HTTP plumbing? → `api/`

---

## 2. Module Public Surface

Every module has an `index.ts` that exports only what other modules need:

```ts
// src/modules/customers/index.ts
export { default as CustomersRouter } from './pages/CustomersRouter';
export { useCustomerOptions } from './hooks/useCustomerOptions';

// Consumed by other modules to display a customer reference (e.g. in a Quotation form):
export type { CustomerSummary } from './types/customer.types';
```

The router is the typical default export so `routes/app-routes.tsx` can lazy-load it:

```tsx
const CustomersRouter = lazy(() => import('@/modules/customers'));
```

### Rules

- **Default export** for the router; **named exports** for everything else.
- **Internal modules are private.** Only `index.ts` is the import path.
- **No barrel-files inside the module.** Components, hooks, schemas, types do not get `index.ts`. Only the module root has one.
- **Re-export only what's needed externally.** A module that exports its 30 internal hooks is mis-using the public surface.

---

## 3. Module Router

Each module owns its routes:

```tsx
// src/modules/customers/pages/CustomersRouter.tsx
import { Routes, Route, Navigate } from 'react-router-dom';
import { CustomersListPage } from './CustomersListPage';
import { CustomerCreatePage } from './CustomerCreatePage';
import { CustomerEditPage } from './CustomerEditPage';
import { CustomerDetailPage } from './CustomerDetailPage';
import { RequirePermission } from '@/auth/require-permission';

export default function CustomersRouter() {
  return (
    <Routes>
      <Route index element={<Navigate to="list" replace />} />
      <Route element={<RequirePermission permission="customers.view" />}>
        <Route path="list" element={<CustomersListPage />} />
        <Route path=":id" element={<CustomerDetailPage />} />
      </Route>
      <Route element={<RequirePermission permission="customers.manage" />}>
        <Route path="create" element={<CustomerCreatePage />} />
        <Route path=":id/edit" element={<CustomerEditPage />} />
      </Route>
    </Routes>
  );
}
```

### Rules

- **Permission-based route guards** are explicit per route.
- **Module router is the only `default export`** in the module.
- **Module owns its URL space** — `/customers/list`, `/customers/:id`, `/customers/create`, `/customers/:id/edit`.
- **Lazy-load** at the routes level (in `src/routes/app-routes.tsx`), not inside the module.

---

## 4. RTK Query Slice (Per Module)

```ts
// src/modules/customers/api/customers.api.ts
import { api } from '@/api/base-query';
import type {
  Customer,
  CustomerDetail,
  CreateCustomerRequest,
  UpdateCustomerRequest,
  SearchCustomersQuery,
} from './customers.types';
import type { PagedResult } from '@/types/api.types';

export const customersApi = api.injectEndpoints({
  endpoints: (build) => ({
    searchCustomers: build.query<PagedResult<Customer>, SearchCustomersQuery>({
      query: ({ search, page = 1, pageSize = 20 }) => ({
        url: '/api/customers',
        params: { search, page, pageSize },
      }),
      providesTags: (result) =>
        result
          ? [
              ...result.items.map((c) => ({ type: 'Customer' as const, id: c.id })),
              { type: 'Customer' as const, id: 'LIST' },
            ]
          : [{ type: 'Customer' as const, id: 'LIST' }],
    }),

    getCustomerById: build.query<CustomerDetail, string>({
      query: (id) => `/api/customers/${id}`,
      providesTags: (_, __, id) => [{ type: 'Customer', id }],
    }),

    createCustomer: build.mutation<Customer, CreateCustomerRequest>({
      query: (body) => ({
        url: '/api/customers',
        method: 'POST',
        body,
      }),
      invalidatesTags: [{ type: 'Customer', id: 'LIST' }],
    }),

    updateCustomer: build.mutation<Customer, { id: string; body: UpdateCustomerRequest }>({
      query: ({ id, body }) => ({
        url: `/api/customers/${id}`,
        method: 'PUT',
        body,
      }),
      invalidatesTags: (_, __, { id }) => [
        { type: 'Customer', id },
        { type: 'Customer', id: 'LIST' },
      ],
    }),

    deleteCustomer: build.mutation<void, string>({
      query: (id) => ({
        url: `/api/customers/${id}`,
        method: 'DELETE',
      }),
      invalidatesTags: (_, __, id) => [
        { type: 'Customer', id },
        { type: 'Customer', id: 'LIST' },
      ],
    }),
  }),
});

export const {
  useSearchCustomersQuery,
  useGetCustomerByIdQuery,
  useCreateCustomerMutation,
  useUpdateCustomerMutation,
  useDeleteCustomerMutation,
} = customersApi;
```

### Rules

- **Use `injectEndpoints`** — modules attach their endpoints to the shared `api`. There is one `createApi` instance for the whole app, in `src/api/base-query.ts`.
- **Tags follow the pattern** `{ type: '<Aggregate>', id: '<id>' | 'LIST' }`.
- **Mutations invalidate** the affected list and item tags.
- **Hooks named** `use<Verb><Resource>Query` for reads, `use<Verb><Resource>Mutation` for writes.
- **Detail in** `02-frontend/03-state-and-data-fetching.md`.

---

## 5. Pages

A page is a thin container that:
- Reads URL params.
- Calls module hooks for data.
- Renders feature components.
- Handles top-level routing (e.g. redirect after submit).

```tsx
// src/modules/customers/pages/CustomerEditPage.tsx
import { useParams, useNavigate } from 'react-router-dom';
import { useGetCustomerByIdQuery, useUpdateCustomerMutation } from '../api/customers.api';
import { CustomerForm } from '../components/CustomerForm';
import { ContentLoader } from '@/components/common/content-loader';
import { ErrorState } from '@/components/feedback/error-state';
import { toast } from '@/components/ui/sonner';

export function CustomerEditPage() {
  const { id = '' } = useParams();
  const navigate = useNavigate();
  const { data, isLoading, error } = useGetCustomerByIdQuery(id, { skip: !id });
  const [updateCustomer, { isLoading: isSaving }] = useUpdateCustomerMutation();

  if (isLoading) return <ContentLoader />;
  if (error || !data) return <ErrorState error={error} />;

  return (
    <CustomerForm
      defaultValues={data}
      isSubmitting={isSaving}
      onSubmit={async (values) => {
        try {
          await updateCustomer({ id, body: values }).unwrap();
          toast.success('Customer updated');
          navigate(`/customers/${id}`);
        } catch (err) {
          toast.error('Failed to update customer');
        }
      }}
    />
  );
}
```

### Rules

- **Pages are < 100 lines.** If they grow, extract logic into a `useCustomerEdit` hook.
- **Pages don't render forms directly** — they delegate to a `CustomerForm` component.
- **Side effects (toast, navigate)** happen at the page level, not inside reusable components.

---

## 6. Components (Feature-Private)

Components in a module are reusable **inside that module only**. They render UI given props.

```tsx
// src/modules/customers/components/CustomerForm.tsx
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { Form, FormField, FormItem, FormLabel, FormControl, FormMessage } from '@/components/ui/form';
import { Input } from '@/components/ui/input';
import { Textarea } from '@/components/ui/textarea';
import { Button } from '@/components/ui/button';
import { customerFormSchema, type CustomerFormValues } from '../schemas/customer.schema';

type CustomerFormProps = Readonly<{
  defaultValues?: Partial<CustomerFormValues>;
  isSubmitting?: boolean;
  onSubmit: (values: CustomerFormValues) => void | Promise<void>;
  onCancel?: () => void;
}>;

export function CustomerForm({
  defaultValues,
  isSubmitting,
  onSubmit,
  onCancel,
}: CustomerFormProps) {
  const form = useForm<CustomerFormValues>({
    resolver: zodResolver(customerFormSchema),
    defaultValues: { name: '', email: '', notes: '', ...defaultValues },
  });

  return (
    <Form {...form}>
      <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
        <FormField
          control={form.control}
          name="name"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Name</FormLabel>
              <FormControl><Input {...field} /></FormControl>
              <FormMessage />
            </FormItem>
          )}
        />
        <FormField
          control={form.control}
          name="email"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Email</FormLabel>
              <FormControl><Input type="email" {...field} /></FormControl>
              <FormMessage />
            </FormItem>
          )}
        />
        <FormField
          control={form.control}
          name="notes"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Notes</FormLabel>
              <FormControl><Textarea {...field} /></FormControl>
              <FormMessage />
            </FormItem>
          )}
        />
        <div className="flex gap-2 justify-end">
          {onCancel && (
            <Button type="button" variant="outline" onClick={onCancel}>Cancel</Button>
          )}
          <Button type="submit" disabled={isSubmitting}>
            {isSubmitting ? 'Saving...' : 'Save'}
          </Button>
        </div>
      </form>
    </Form>
  );
}
```

### Rules

- **Function components only.** No class components.
- **Props are `Readonly<...>`.**
- **Named exports only** (no `export default` except in the module's public router).
- **Decoupled from data fetching.** A form component receives `defaultValues` and `onSubmit` — it doesn't call RTK Query directly.
- **Detail in** `02-frontend/06-coding-standards.md`.

---

## 7. Hooks (Feature-Private)

Custom hooks compose RTK Query hooks, derived state, or local UI state:

```ts
// src/modules/customers/hooks/useCustomerFilters.ts
import { useState } from 'react';
import { useDebouncedValue } from '@/hooks/useDebouncedValue';

export function useCustomerFilters() {
  const [search, setSearch] = useState('');
  const debouncedSearch = useDebouncedValue(search, 300);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(20);

  function reset() {
    setSearch('');
    setPage(1);
  }

  return {
    search,
    debouncedSearch,
    setSearch,
    page,
    setPage,
    pageSize,
    setPageSize,
    reset,
  };
}
```

### Rules

- **One hook per file.** File name matches the hook (`useCustomerFilters.ts`).
- **Hooks return an object** with named fields, never a tuple unless it's the React idiom (e.g. `[value, setValue]`).
- **Side effects (toast, navigation) belong in the page**, not the hook.

---

## 8. Schemas

Zod schemas live in `schemas/` and are the single source of truth for form validation:

```ts
// src/modules/customers/schemas/customer.schema.ts
import { z } from 'zod';

export const customerFormSchema = z.object({
  name: z.string().trim().min(1, 'Name is required').max(200),
  email: z.string().trim().toLowerCase().email('Must be a valid email').max(320),
  notes: z.string().max(1000).optional(),
});

export type CustomerFormValues = z.infer<typeof customerFormSchema>;
```

### Rules

- **One schema per form.** Don't reuse a single mega-schema for every form variation.
- **Schemas live in `schemas/`** — never inside components.
- **Inferred types** (`z.infer<typeof ...>`) are exported alongside the schema.
- **Detail in** `02-frontend/04-forms-and-validation.md`.

---

## 9. Types

API request/response types live in `api/<feature>.types.ts` (or are generated from the backend's OpenAPI spec):

```ts
// src/modules/customers/api/customers.types.ts
export type CustomerStatus = 'Active' | 'Inactive';

export type Customer = {
  id: string;
  name: string;
  email: string;
  status: CustomerStatus;
  createdAtUtc: string;
};

export type CustomerDetail = Customer & {
  notes?: string | null;
  contactPersons: ContactPerson[];
  updatedAtUtc: string;
};

export type ContactPerson = {
  id: string;
  name: string;
  phoneNumber: string;
  isPrimary: boolean;
};

export type CreateCustomerRequest = {
  name: string;
  email: string;
  notes?: string;
};

export type UpdateCustomerRequest = CreateCustomerRequest;

export type SearchCustomersQuery = {
  search?: string;
  page?: number;
  pageSize?: number;
};
```

### Rules

- **`type` over `interface`** for object shapes.
- **No `enum`s.** Use string-literal unions: `type Status = 'Active' | 'Inactive'`.
- **Discriminated unions** for state machines and remote-data patterns.
- **Generated from OpenAPI** when possible — `openapi-typescript` outputs to `src/api/generated/types.ts` and modules import from there.

---

## 10. The "Index Per Module" Rule

```
src/modules/customers/
├── index.ts                          ← Public surface (one barrel)
├── pages/CustomersRouter.tsx          ← Default export of index.ts
├── api/customers.api.ts               ← Imported via './api/customers.api' relative
├── components/                        ← No index.ts here
│   ├── CustomerForm.tsx
│   └── CustomerList.tsx
└── ...
```

### Rules

- **Exactly one `index.ts` per module**, at the module root.
- **No `index.ts` in subfolders.** Components, hooks, schemas import each other by direct file path.
- **Why:** the module root's `index.ts` is the contract with the rest of the app. Sub-barrels create circular import risk and obscure refactoring.

---

## 11. Common Mistakes

| Mistake                                                         | Fix                                                              |
|------------------------------------------------------------------|------------------------------------------------------------------|
| `import { Foo } from '@/modules/customers/components/Foo'`       | Either expose `Foo` from `index.ts` or relocate it to `components/`|
| Default export from a non-router file                            | Named exports                                                    |
| Form schema duplicated between FE and BE                         | Generate types from OpenAPI; one source of truth                 |
| Page > 200 lines mixing data fetching + UI                       | Extract a `use<Page>` hook + smaller components                  |
| Module router lazy-loaded inside the module                      | Lazy-load at `routes/app-routes.tsx`                             |
| Sharing internal hooks across modules                            | Promote to `src/hooks/` if truly cross-cutting                   |
| Re-exporting every internal file from `index.ts`                 | Export only what others need                                     |
