# Forms and Validation

Forms are built on **React Hook Form** + **Zod** + the **shadcn-style `Form` component** that ships with Metronic. The schema is the single source of truth for form structure, validation, and TypeScript types.

---

## 1. The Pattern

```
┌──────────────────────┐
│  Zod schema (.schema.ts)  ← validation + types
└─────────┬────────────┘
          ▼
┌────────────────────────────────────┐
│  useForm<T>({ resolver: zodResolver(schema) })  │
└─────────┬──────────────────────────┘
          ▼
┌────────────────────────────────────┐
│  <Form> + <FormField> components    │   ← from src/components/ui/form.tsx
└─────────┬──────────────────────────┘
          ▼
┌────────────────────────────────────┐
│  onSubmit → RTK Query mutation      │
└────────────────────────────────────┘
```

---

## 2. Schema First

```ts
// src/modules/customers/schemas/customer.schema.ts
import { z } from 'zod';

export const customerFormSchema = z.object({
  name: z
    .string()
    .trim()
    .min(1, 'Name is required')
    .max(200, 'Name must be at most 200 characters'),
  email: z
    .string()
    .trim()
    .toLowerCase()
    .email('Must be a valid email address')
    .max(320),
  phoneNumber: z
    .string()
    .trim()
    .regex(/^\+?[1-9]\d{1,14}$/, 'Must be a valid international phone number')
    .optional()
    .or(z.literal('')),
  notes: z.string().max(1000).optional(),
  contactPersons: z
    .array(
      z.object({
        name: z.string().trim().min(1),
        phoneNumber: z.string().trim().min(1),
        isPrimary: z.boolean(),
      })
    )
    .max(10),
});

export type CustomerFormValues = z.infer<typeof customerFormSchema>;
```

### Rules

- **Schemas live in `modules/<feature>/schemas/`.** One file per major form.
- **Trim and normalize** at parse time (`.trim()`, `.toLowerCase()`).
- **`.optional()` vs `''`**: optional fields use `.optional().or(z.literal(''))` so the empty string is treated as "not provided" — useful for HTML forms where empty input fields produce `''`.
- **Inferred type** is exported alongside the schema.
- **Schema mirrors the backend's FluentValidation rules** as closely as possible. Backend is the authority on persistence-critical rules; frontend gives early UX feedback.

---

## 3. The `Form` Component (Metronic / shadcn)

`src/components/ui/form.tsx` ships with Metronic. It composes:

- `Form` — wrapper providing form context.
- `FormField` — connects RHF's `control` to a single input.
- `FormItem`, `FormLabel`, `FormControl`, `FormDescription`, `FormMessage` — semantic structure.

Don't reinvent these. Use them directly.

---

## 4. Form Component Pattern

```tsx
// src/modules/customers/components/CustomerForm.tsx
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { Button } from '@/components/ui/button';
import { Form, FormField, FormItem, FormLabel, FormControl, FormMessage } from '@/components/ui/form';
import { Input } from '@/components/ui/input';
import { Textarea } from '@/components/ui/textarea';
import { customerFormSchema, type CustomerFormValues } from '../schemas/customer.schema';

type CustomerFormProps = Readonly<{
  defaultValues?: Partial<CustomerFormValues>;
  isSubmitting?: boolean;
  onSubmit: (values: CustomerFormValues) => void | Promise<void>;
  onCancel?: () => void;
  submitLabel?: string;
}>;

export function CustomerForm({
  defaultValues,
  isSubmitting,
  onSubmit,
  onCancel,
  submitLabel = 'Save',
}: CustomerFormProps) {
  const form = useForm<CustomerFormValues>({
    resolver: zodResolver(customerFormSchema),
    mode: 'onTouched',     // validate after first blur, then on every change
    defaultValues: {
      name: '',
      email: '',
      phoneNumber: '',
      notes: '',
      contactPersons: [],
      ...defaultValues,
    },
  });

  return (
    <Form {...form}>
      <form
        onSubmit={form.handleSubmit(onSubmit)}
        className="space-y-4"
        noValidate
      >
        <FormField
          control={form.control}
          name="name"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Name</FormLabel>
              <FormControl>
                <Input placeholder="Acme Hospital" autoComplete="organization" {...field} />
              </FormControl>
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
              <FormControl>
                <Input type="email" autoComplete="email" {...field} />
              </FormControl>
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
              <FormControl><Textarea rows={4} {...field} /></FormControl>
              <FormMessage />
            </FormItem>
          )}
        />

        <div className="flex justify-end gap-2">
          {onCancel && (
            <Button type="button" variant="outline" onClick={onCancel} disabled={isSubmitting}>
              Cancel
            </Button>
          )}
          <Button type="submit" disabled={isSubmitting}>
            {isSubmitting ? 'Saving...' : submitLabel}
          </Button>
        </div>
      </form>
    </Form>
  );
}
```

### Rules

- **`noValidate`** on the `<form>` — disables browser default validation; Zod is the source of truth.
- **`mode: 'onTouched'`** — validates after first blur. `'onBlur'` is too lazy; `'onChange'` is too noisy.
- **Default values include all fields** — RHF complains about uncontrolled-to-controlled transitions otherwise.
- **`autoComplete` attributes** for accessibility and password-manager support (`email`, `organization`, `tel`, `street-address`).
- **Form is dumb**: receives `defaultValues` and `onSubmit`. Doesn't call RTK Query directly.
- **Submit button shows in-flight state** based on the parent's `isSubmitting`.

---

## 5. Server-Side Errors → Field-Level Errors

When the backend returns a 400 with a per-field validation breakdown, surface it in the form:

```ts
import { isValidationError } from '@/api/api-error';

async function handleSubmit(values: CustomerFormValues) {
  try {
    await createCustomer(values).unwrap();
    toast.success('Customer created');
    navigate('/customers');
  } catch (err) {
    if (isValidationError(err)) {
      // Walk the field map and set RHF errors:
      Object.entries(err.fields).forEach(([field, messages]) => {
        form.setError(field as keyof CustomerFormValues, {
          type: 'server',
          message: messages.join(', '),
        });
      });
      return;
    }
    toast.error('Failed to create customer');
  }
}
```

### Rules

- **`form.setError`** for field-level errors so they appear under the right input.
- **Lowercase the backend's `PropertyName`** if the schema uses camelCase. The backend ProblemDetails should already use camelCase for field names — coordinate this.
- **Toast for non-field errors** (network, conflict). Don't silently swallow.

---

## 6. Field Arrays (Repeated Sections)

For dynamic lists (contact persons, line items, etc.):

```tsx
import { useFieldArray, useFormContext } from 'react-hook-form';

export function ContactPersonsFields() {
  const { control } = useFormContext<CustomerFormValues>();
  const { fields, append, remove } = useFieldArray({ control, name: 'contactPersons' });

  return (
    <div className="space-y-3">
      <div className="flex items-center justify-between">
        <h4 className="font-medium">Contact persons</h4>
        <Button
          type="button"
          variant="ghost"
          onClick={() => append({ name: '', phoneNumber: '', isPrimary: false })}
        >
          + Add
        </Button>
      </div>

      {fields.map((field, index) => (
        <div key={field.id} className="grid grid-cols-12 gap-2 items-end">
          <FormField
            control={control}
            name={`contactPersons.${index}.name`}
            render={({ field }) => (
              <FormItem className="col-span-5">
                <FormLabel>Name</FormLabel>
                <FormControl><Input {...field} /></FormControl>
                <FormMessage />
              </FormItem>
            )}
          />
          <FormField
            control={control}
            name={`contactPersons.${index}.phoneNumber`}
            render={({ field }) => (
              <FormItem className="col-span-5">
                <FormLabel>Phone</FormLabel>
                <FormControl><Input {...field} /></FormControl>
                <FormMessage />
              </FormItem>
            )}
          />
          <Button
            type="button"
            variant="destructive"
            className="col-span-2"
            onClick={() => remove(index)}
          >
            Remove
          </Button>
        </div>
      ))}
    </div>
  );
}
```

### Rules

- **`useFieldArray`** is the standard.
- **`field.id`** as React `key` — never the array index.
- **Limit the array length** in the schema (`.max(N)`).
- **Provide an "Add" button** with a sensible default object.

---

## 7. Async Validation (Field-Level)

When you need to check uniqueness against the server (e.g. "email already in use"):

```ts
const customerFormSchema = z.object({
  email: z
    .string()
    .email()
    .refine(
      async (email) => {
        // Avoid hammering on every keystroke — combine with debouncing in the UI.
        const res = await api.get(`/api/customers/email-available?email=${encodeURIComponent(email)}`);
        return res.data.available;
      },
      { message: 'Email is already in use' }
    ),
});
```

### Rules

- **Use sparingly.** Async refinements run on every form validation and add latency.
- **Debounce the input** (`useDebouncedValue`) before triggering validation.
- **Always have a server-side check** as the final guard. Async validation is UX, not security.

---

## 8. Date Pickers, Selects, Multi-Selects

The Metronic theme ships with these primitives. Use them via `FormField`:

```tsx
<FormField
  control={form.control}
  name="customerId"
  render={({ field }) => (
    <FormItem>
      <FormLabel>Customer</FormLabel>
      <Select value={field.value} onValueChange={field.onChange}>
        <FormControl>
          <SelectTrigger>
            <SelectValue placeholder="Choose a customer" />
          </SelectTrigger>
        </FormControl>
        <SelectContent>
          {customers?.map(c => (
            <SelectItem key={c.id} value={c.id}>{c.name}</SelectItem>
          ))}
        </SelectContent>
      </Select>
      <FormMessage />
    </FormItem>
  )}
/>
```

### Rules

- **Selects use IDs as values.** Never names. Resolve to display text in the rendered options.
- **Date pickers store ISO strings or `Date` objects** — match the schema. Be explicit: backend expects ISO 8601.
- **Multi-selects** use array fields validated by `z.array(z.string()).min(1)`.

---

## 9. Reset, Dirty Detection, Unsaved-Changes Guard

```ts
const isDirty = form.formState.isDirty;

useEffect(() => {
  if (!isDirty) return;
  const handler = (e: BeforeUnloadEvent) => {
    e.preventDefault();
    e.returnValue = '';
  };
  window.addEventListener('beforeunload', handler);
  return () => window.removeEventListener('beforeunload', handler);
}, [isDirty]);
```

For in-app navigation guards, use React Router's `useBlocker` or a custom prompt component.

### Rules

- **Show a confirmation prompt** when leaving a dirty form.
- **`form.reset(newDefaults)`** after a successful mutation if the form should retain values; `form.reset()` clears.

---

## 10. Submission Guarding

Prevent double-submit:

```tsx
const [createCustomer, { isLoading }] = useCreateCustomerMutation();

<Button type="submit" disabled={isLoading || !form.formState.isValid}>
  {isLoading ? 'Saving...' : 'Save'}
</Button>
```

### Rules

- **Disable submit while in-flight** (`isLoading`).
- **Don't disable on `!isValid`** as the only check — RHF reports `isValid: false` until the user has touched fields. Combine with `mode: 'onTouched'`.

---

## 11. Accessibility

- **Every input has an associated label.** `FormLabel` does this for you.
- **Errors are announced.** `FormMessage` includes `aria-live="polite"`.
- **Tab order is logical** — DOM order matches visual order.
- **Required fields** indicated visually (asterisk) and via `aria-required`.
- **Submit on Enter** in single-line inputs; Ctrl+Enter for textareas.

---

## 12. Testing Forms

```tsx
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { CustomerForm } from './CustomerForm';

it('shows validation errors when submitting empty', async () => {
  const onSubmit = vi.fn();
  render(<CustomerForm onSubmit={onSubmit} />);

  await userEvent.click(screen.getByRole('button', { name: /save/i }));

  expect(await screen.findByText(/name is required/i)).toBeInTheDocument();
  expect(onSubmit).not.toHaveBeenCalled();
});

it('calls onSubmit with parsed values', async () => {
  const onSubmit = vi.fn();
  render(<CustomerForm onSubmit={onSubmit} />);

  await userEvent.type(screen.getByLabelText(/name/i), 'Alice');
  await userEvent.type(screen.getByLabelText(/email/i), 'Alice@Example.com');
  await userEvent.click(screen.getByRole('button', { name: /save/i }));

  expect(onSubmit).toHaveBeenCalledWith(expect.objectContaining({
    name: 'Alice',
    email: 'alice@example.com',  // schema lowercases
  }));
});
```

### Rules

- **`getByRole`, `getByLabel`, `getByText`** — accessible queries.
- **`userEvent`** over `fireEvent` — simulates real user interaction.
- **Test the public contract**: what the user sees, what `onSubmit` receives. Don't test RHF internals.

---

## 13. Common Mistakes

| Mistake                                                              | Fix                                                              |
|----------------------------------------------------------------------|------------------------------------------------------------------|
| `useState` for each form field                                       | Use `useForm` with one schema                                    |
| Validation logic inside the component                                | Move to a Zod schema                                             |
| Forgetting `defaultValues` for some fields                           | Initialize every field, even to `''`                             |
| Not handling server-side validation errors                           | Map ProblemDetails `errors` to `form.setError`                   |
| Submitting on `Enter` in a textarea                                  | Use `Ctrl+Enter` (default browser behavior)                      |
| Index as `key` in field arrays                                       | `field.id` from `useFieldArray`                                  |
| Async refinement without debouncing                                  | Debounce the input upstream                                      |
| Form component calls `useMutation` directly                          | Pass `onSubmit` from the parent page                             |
| Mixing schema validation with `required` HTML attribute              | `noValidate`; let Zod handle it                                  |
| Forgetting `autoComplete` attributes                                 | Add `autoComplete="email"`, etc.                                 |
