# Error Responses

All errors follow **RFC 9457 ProblemDetails**. The frontend parses one shape; the backend produces one shape.

---

## 1. ProblemDetails Shape

```json
{
  "type":    "https://httpstatuses.io/<status>",
  "title":   "<short, stable title>",
  "status":  <number>,
  "detail":  "<human-readable, optionally with detail>",
  "instance":"<request URL>",
  "traceId": "<correlation id>"
}
```

### Field rules

- **`type`**: a URL identifying the error category. We use `https://httpstatuses.io/<n>` as a stable default.
- **`title`**: short, stable, machine-friendly. `"Not Found"`, `"Validation failed"`, `"Conflict"`. Don't change between releases.
- **`status`**: matches the HTTP status code.
- **`detail`**: human-readable. Safe to display to the user.
- **`instance`**: the request URL that caused the error.
- **`traceId`**: the request's `X-Correlation-Id`. Surface it in the UI on 5xx so users can quote it in support tickets.

---

## 2. Status Codes (and their ProblemDetails titles)

| Status | Title                       | Cause                                                  |
|--------|-----------------------------|--------------------------------------------------------|
| 400    | Validation failed           | Field-level validation failed                          |
| 401    | Unauthorized                | Missing / invalid token                                |
| 403    | Forbidden                   | Authenticated but lacks permission                     |
| 404    | Not Found                   | Resource doesn't exist                                 |
| 409    | Conflict                    | Concurrency / duplicate / state-conflict               |
| 422    | Domain rule violated        | Business rule rejected the request                     |
| 429    | Too Many Requests           | Rate limit exceeded                                    |
| 500    | Internal Server Error       | Unhandled exception                                    |

### Rules

- **Title is stable.** Frontend code can match on `title` — don't reword between releases.
- **Don't expose stack traces** in `detail` outside development.

---

## 3. Validation Errors (400) — Extended Shape

```json
{
  "type":    "https://httpstatuses.io/400",
  "title":   "Validation failed",
  "status":  400,
  "errors": {
    "email":         ["Email must be a valid address."],
    "name":          ["Name is required."],
    "contactPersons.0.phoneNumber": ["Phone is required."]
  },
  "traceId": "..."
}
```

### Rules

- **`errors` is a `Record<string, string[]>`** — field name → array of messages.
- **Field names use camelCase**, matching the request body.
- **Nested fields use dot notation**: `contactPersons.0.phoneNumber`.
- **Multiple messages per field** are allowed.

---

## 4. Backend Mapping

In `GlobalExceptionMiddleware` (see `01-backend/05-api-layer.md` §4):

```csharp
var (status, title, problem) = ex switch
{
    FluentValidation.ValidationException ve => (
        StatusCodes.Status400BadRequest,
        "Validation failed",
        BuildValidationProblem(ve)),

    NotFoundException nf => (
        StatusCodes.Status404NotFound,
        "Not Found",
        new ProblemDetails { Detail = nf.Message }),

    ConflictException cf => (
        StatusCodes.Status409Conflict,
        "Conflict",
        new ProblemDetails { Detail = cf.Message }),

    ForbiddenException fb => (
        StatusCodes.Status403Forbidden,
        "Forbidden",
        new ProblemDetails { Detail = fb.Message }),

    DomainException de => (
        StatusCodes.Status422UnprocessableEntity,
        "Domain rule violated",
        new ProblemDetails { Detail = de.Message }),

    UnauthorizedAccessException => (
        StatusCodes.Status401Unauthorized,
        "Unauthorized",
        new ProblemDetails { Detail = "Authentication is required." }),

    DbUpdateConcurrencyException => (
        StatusCodes.Status409Conflict,
        "Conflict",
        new ProblemDetails { Detail = "The resource was modified by another user. Please reload and try again." }),

    _ => (
        StatusCodes.Status500InternalServerError,
        "Internal Server Error",
        new ProblemDetails { Detail = env.IsDevelopment() ? ex.ToString() : "An unexpected error occurred." })
};

problem.Status = status;
problem.Title = title;
problem.Type = $"https://httpstatuses.io/{status}";
problem.Instance = context.Request.Path;
problem.Extensions["traceId"] = context.Response.Headers["X-Correlation-Id"].ToString();
```

---

## 5. Frontend Parsing

```ts
// src/api/api-error.ts
export type ApiError =
  | { kind: 'network' }
  | { kind: 'validation'; fields: Record<string, string[]> }
  | { kind: 'unauthorized' }
  | { kind: 'forbidden'; message: string }
  | { kind: 'notFound'; message: string }
  | { kind: 'conflict'; message: string }
  | { kind: 'domainRule'; message: string }
  | { kind: 'rateLimited'; retryAfter?: number }
  | { kind: 'server'; traceId?: string; message: string };

export function parseProblemDetails(status: number, body: unknown, headers: Headers): ApiError {
  const pd = (body ?? {}) as { detail?: string; errors?: Record<string, string[]>; traceId?: string };
  switch (status) {
    case 400:
      return { kind: 'validation', fields: pd.errors ?? {} };
    case 401:
      return { kind: 'unauthorized' };
    case 403:
      return { kind: 'forbidden', message: pd.detail ?? 'Forbidden' };
    case 404:
      return { kind: 'notFound', message: pd.detail ?? 'Not Found' };
    case 409:
      return { kind: 'conflict', message: pd.detail ?? 'Conflict' };
    case 422:
      return { kind: 'domainRule', message: pd.detail ?? 'Rule violation' };
    case 429:
      return { kind: 'rateLimited', retryAfter: Number(headers.get('Retry-After')) || undefined };
    default:
      return {
        kind: 'server',
        traceId: pd.traceId,
        message: pd.detail ?? 'Server error',
      };
  }
}

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
    case 'conflict':      return err.message || 'There was a conflict. Please reload and try again.';
    case 'domainRule':    return err.message;
    case 'rateLimited':   return `Too many requests. Try again ${err.retryAfter ? `in ${err.retryAfter}s` : 'later'}.`;
    case 'server':        return `Something went wrong. Reference: ${err.traceId ?? 'unknown'}.`;
  }
}
```

### Rules

- **`baseQuery` parses ProblemDetails** on every error response, returning a typed `ApiError`.
- **Components type errors as `ApiError`**, never `unknown`.
- **`toUserMessage(err)`** is the standard "show a toast" helper.

---

## 6. Per-Field Errors in Forms

When a 400 comes back, surface errors next to the right fields:

```ts
async function handleSubmit(values: Form) {
  try {
    await mutate(values).unwrap();
  } catch (err) {
    if (isValidationError(err)) {
      Object.entries(err.fields).forEach(([field, messages]) => {
        form.setError(field as keyof Form, {
          type: 'server',
          message: messages.join(', '),
        });
      });
      return;
    }
    toast.error(toUserMessage(err));
  }
}
```

### Rules

- **Field-level errors → `form.setError`.**
- **Top-level errors → `toast.error`.**
- **Always handle `isValidationError` first** — it's the only error with field-level data.

---

## 7. Showing 5xx Errors

For unhandled exceptions, show a friendly screen with the trace ID:

```tsx
function ErrorState({ error }: { error: ApiError | unknown }) {
  if (!isApiError(error)) {
    return <p>Something went wrong. Please refresh.</p>;
  }
  if (error.kind === 'server') {
    return (
      <div role="alert" className="rounded-md border border-danger/30 bg-danger/5 p-4">
        <h3 className="font-medium text-danger">Something went wrong</h3>
        <p className="text-sm text-muted-foreground">
          We're looking into it. Reference: <code>{error.traceId ?? 'unknown'}</code>
        </p>
        <Button onClick={() => location.reload()} className="mt-3">Reload</Button>
      </div>
    );
  }
  return <p>{toUserMessage(error)}</p>;
}
```

### Rules

- **Show the trace ID for 5xx**, not for 4xx — 4xx are user-fixable.
- **Provide a retry action** when sensible.
- **Don't loop retries automatically** for 5xx — let the user choose.

---

## 8. Bilingual / Localized Messages

If the app is localized, the backend returns **machine-friendly stable identifiers** in `title`, and the frontend looks up the translated message:

Backend:
```json
{ "title": "validation_failed", "errors": { "email": ["email_invalid"] } }
```

Frontend:
```ts
const messages = {
  validation_failed: { en: 'Validation failed', tr: 'Doğrulama başarısız' },
  email_invalid: { en: 'Email is invalid', tr: 'Email geçersiz' },
};
```

### Rules

- **Default to English in `detail`** when no localization is needed.
- **For localized projects**, agree on identifiers up-front and document them in a shared file.

---

## 9. Common Mistakes

| Mistake                                                          | Fix                                                              |
|------------------------------------------------------------------|------------------------------------------------------------------|
| Multiple error formats across endpoints                          | Always ProblemDetails                                            |
| Title that changes per release                                   | Stable title + variable detail                                   |
| Stack trace in `detail` in production                            | Hide; show only `traceId`                                        |
| Missing `traceId` for 5xx                                        | Always include for support handoff                               |
| Validation errors as a flat string                               | `errors` map with field names                                    |
| Field names in `PascalCase`                                      | Match request body — camelCase                                   |
| Frontend showing raw API messages                                | Use `toUserMessage(err)` mapping                                 |
| Mixing HTTP status meanings (e.g. `400` for "not found")         | Use the right code                                               |
| Auto-retrying 4xx                                                | Only retry network and 5xx — and only for idempotent verbs       |
