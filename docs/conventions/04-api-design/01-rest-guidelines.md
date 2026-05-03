# REST API Guidelines

This document defines how endpoints are shaped: URLs, methods, status codes, headers, request/response bodies.

---

## 1. Resource Naming

| Pattern                                  | Example                              |
|------------------------------------------|--------------------------------------|
| Plural nouns for collections             | `/api/customers`                     |
| Resource ID for items                    | `/api/customers/{id}`                |
| Sub-resources nested                     | `/api/customers/{id}/contact-persons` |
| Verb-style for non-CRUD actions          | `/api/customers/{id}/deactivate` (POST) |
| Lowercase, kebab-case                    | `/api/sales-orders`, `/api/amc-contracts` |
| URL versioning                           | `/api/v1/customers` (when needed)    |

### Rules

- **Collections are plural.** `/customers`, not `/customer`.
- **Item URLs use the resource ID** (UUID or business key). `/customers/{id}`.
- **Sub-resources reflect ownership.** `/customers/{id}/invoices` — invoices belonging to a customer.
- **Actions that aren't CRUD** use a verb segment: `/customers/{id}/deactivate`, `/quotations/{id}/convert-to-sales-order`. Use `POST`.
- **Avoid deep nesting.** Two levels max: `/customers/{id}/invoices/{invoiceId}` is the limit. Beyond that, switch to a flat resource: `/invoices/{invoiceId}`.

---

## 2. HTTP Methods

| Method   | Purpose                                  | Idempotent | Body                             |
|----------|------------------------------------------|------------|----------------------------------|
| `GET`    | Read a resource or collection            | Yes        | No                               |
| `POST`   | Create a resource; trigger an action     | No         | Yes                              |
| `PUT`    | Replace a resource (full update)         | Yes        | Yes (all fields)                 |
| `PATCH`  | Partial update                           | Yes (preferably) | Yes (changed fields only)  |
| `DELETE` | Delete a resource                        | Yes        | No                               |

### Rules

- **`PUT` replaces the entire resource.** Send all fields, even unchanged ones.
- **`PATCH` updates partial fields.** Use JSON Merge Patch (RFC 7396) for simple cases. Avoid JSON Patch (RFC 6902) — too complex.
- **Idempotency**: `GET`, `PUT`, `DELETE` are inherently idempotent. `POST` mutations carry an `Idempotency-Key` header (see §6).

---

## 3. Standard Status Codes

| Code | Meaning                                       | When to use                                                   |
|------|-----------------------------------------------|---------------------------------------------------------------|
| 200  | OK                                            | Successful GET / PUT / PATCH; POST returning data not Created |
| 201  | Created                                       | Successful POST that created a resource. Include `Location` header |
| 202  | Accepted                                      | Async operation queued                                        |
| 204  | No Content                                    | Successful DELETE; PUT/PATCH that returns no body             |
| 400  | Bad Request                                   | Validation failure                                            |
| 401  | Unauthorized                                  | No or invalid auth token                                      |
| 403  | Forbidden                                     | Authenticated but lacks permission                            |
| 404  | Not Found                                     | Resource doesn't exist                                        |
| 409  | Conflict                                      | Concurrency conflict, duplicate key, business rule violation  |
| 422  | Unprocessable Entity                          | Domain rule violation (semantically valid but rejected)       |
| 429  | Too Many Requests                             | Rate limit exceeded                                           |
| 500  | Internal Server Error                         | Unhandled exception                                           |
| 503  | Service Unavailable                           | Health check failed; downstream dependency down               |

### Rules

- **`201` includes `Location`** header pointing to the new resource.
- **`204` for "delete succeeded"**, never `200` with empty body.
- **`409` vs `422`**: 409 for *conflict* with current state ("name already exists"). 422 for *rejected* business rule ("cannot deactivate inactive customer").
- **Never `200` for failures.** The status code is the contract.

---

## 4. Standard Headers

### Request

| Header                  | Required for          | Purpose                                              |
|-------------------------|-----------------------|------------------------------------------------------|
| `Authorization`         | Authenticated requests| `Bearer <jwt>`                                       |
| `Content-Type`          | POST/PUT/PATCH        | `application/json`                                   |
| `Accept`                | Optional              | `application/json`                                   |
| `Idempotency-Key`       | Mutations             | Client-generated UUIDv7                              |
| `X-Correlation-Id`      | All                   | Echo'd by server; UUIDv7 if not present              |
| `If-Match`              | Conditional updates   | ETag for optimistic concurrency (advanced)           |

### Response

| Header                  | When set              | Purpose                                              |
|-------------------------|-----------------------|------------------------------------------------------|
| `Content-Type`          | Always                | `application/json` or `application/problem+json`     |
| `Location`              | 201 Created           | URL of the new resource                              |
| `X-Correlation-Id`      | Always                | Mirror of request, or generated if missing           |
| `Retry-After`           | 429, 503              | Seconds to wait before retrying                      |
| `RateLimit-*` (RFC 9651)| When relevant         | `RateLimit-Limit`, `RateLimit-Remaining`, `RateLimit-Reset` |

---

## 5. Request and Response Bodies

### Always JSON

- `Content-Type: application/json` (or `application/problem+json` for errors).
- `camelCase` property names.
- ISO 8601 timestamps with `Z` for UTC: `"2026-05-02T14:30:00Z"`.

### Standard collection response

```json
{
  "items": [
    { "id": "...", "name": "Alice Inc", "email": "alice@example.com" },
    { "id": "...", "name": "Bob Ltd", "email": "bob@example.com" }
  ],
  "page": 1,
  "pageSize": 20,
  "total": 47
}
```

### Standard item response

```json
{
  "id": "0192...",
  "name": "Alice Inc",
  "email": "alice@example.com",
  "status": "Active",
  "contactPersons": [
    { "id": "0193...", "name": "Alice", "phoneNumber": "+1...", "isPrimary": true }
  ],
  "createdAtUtc": "2026-05-02T14:30:00Z",
  "updatedAtUtc": "2026-05-02T14:30:00Z"
}
```

### Standard error response (RFC 9457 ProblemDetails)

```json
{
  "type": "https://httpstatuses.io/409",
  "title": "Conflict",
  "status": 409,
  "detail": "Customer with email 'alice@example.com' already exists.",
  "instance": "/api/customers",
  "traceId": "0HMSAGCNNAR4M:00000001"
}
```

### Standard validation error

```json
{
  "type": "https://httpstatuses.io/400",
  "title": "Validation failed",
  "status": 400,
  "errors": {
    "email": ["Email must be a valid address."],
    "name": ["Name is required."]
  },
  "traceId": "..."
}
```

### Rules

- **One shape per endpoint.** `GET /customers` returns `{items, page, pageSize, total}`, never sometimes a bare array.
- **Don't wrap successful responses** in `{ data: ... }`. Return the resource directly.
- **Errors always use ProblemDetails.** No mixing of error formats.

---

## 6. Idempotency

Mutations that aren't naturally idempotent (i.e. `POST`) accept an `Idempotency-Key` header:

```http
POST /api/invoices HTTP/1.1
Idempotency-Key: 01923f5c-4b7a-7b8e-9f3d-1a2b3c4d5e6f
Content-Type: application/json
```

The server stores the response in Redis keyed by `(user_id, endpoint, idempotency_key)` with a 24-hour TTL.

- **First request**: process normally, cache response.
- **Subsequent identical requests**: return the cached response.
- **Different body, same key**: respond `409 Conflict`.

```csharp
// Api/Middleware/IdempotencyMiddleware.cs (sketch)
public sealed class IdempotencyMiddleware(RequestDelegate next, ICacheService cache)
{
    public async Task InvokeAsync(HttpContext ctx)
    {
        if (ctx.Request.Method != "POST" && ctx.Request.Method != "DELETE")
        { await next(ctx); return; }

        var key = ctx.Request.Headers["Idempotency-Key"].FirstOrDefault();
        if (key is null) { await next(ctx); return; }

        var userId = ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "anon";
        var cacheKey = $"idem:{userId}:{ctx.Request.Path}:{key}";

        var cached = await cache.GetAsync<CachedResponse>(cacheKey, ctx.RequestAborted);
        if (cached is not null)
        {
            ctx.Response.StatusCode = cached.Status;
            await ctx.Response.WriteAsync(cached.Body);
            return;
        }

        // Capture response, then cache
        var memory = new MemoryStream();
        var original = ctx.Response.Body;
        ctx.Response.Body = memory;

        await next(ctx);

        memory.Position = 0;
        var body = await new StreamReader(memory).ReadToEndAsync();
        await cache.SetAsync(cacheKey, new CachedResponse(ctx.Response.StatusCode, body), TimeSpan.FromHours(24), ctx.RequestAborted);

        memory.Position = 0;
        await memory.CopyToAsync(original);
    }
}
```

### Rules

- **Frontend generates UUIDv7** as the key, sent with every mutation.
- **Cache TTL is 24 hours.** Long enough for genuine retries; short enough to bound memory.
- **Different request body with same key returns 409.**

---

## 7. Filtering, Sorting, Pagination

Detail in `04-api-design/02-pagination-filtering.md`. Quick:

```
GET /api/customers?search=alice&status=Active&sort=-createdAtUtc&page=2&pageSize=20
```

- **Pagination**: `page` (1-based), `pageSize` (max 100, default 20).
- **Sorting**: `sort=field` ascending, `sort=-field` descending. Multi-sort: `sort=-createdAtUtc,name`.
- **Filtering**: simple equality via query string. Complex filters use a dedicated POST endpoint.

---

## 8. Versioning

Use **URL versioning** when breaking changes are needed:

```
/api/v1/customers
/api/v2/customers
```

### Rules

- **Default to no version** for new APIs. Don't preemptively version.
- **When a breaking change ships**, introduce `v2` and keep `v1` running until clients migrate (typically 90 days).
- **Non-breaking additions** (new optional fields, new endpoints) don't bump the version.
- **Deprecation header** on old version: `Deprecation: Sun, 01 Jan 2027 00:00:00 GMT`.

---

## 9. Authentication

- **`Authorization: Bearer <jwt>`** for authenticated requests.
- **Refresh token** in HTTP-only cookie (preferred) or in an authenticated `/auth/refresh` endpoint.
- **Detail in** `05-security/01-authentication-and-authorization.md`.

---

## 10. CORS

- **No `Access-Control-Allow-Origin: *`** in production.
- **Whitelist origins** via `appsettings.json`.
- **`Access-Control-Allow-Credentials: true`** when using cookies — incompatible with `*`.
- **Expose `X-Correlation-Id`** so the frontend can log it.

---

## 11. Caching

### Write endpoints

- **No caching.** Always `Cache-Control: no-store`.

### Read endpoints with public data

- **Short server-side cache** in Redis (e.g. dashboard widgets) — invalidated by domain events.
- **Conditional GET**: `ETag` header + `If-None-Match` for low-bandwidth clients (advanced).

### Rules

- **Default to no caching.** Add caching only where measured latency justifies it.
- **Server-side cache invalidation** is automatic via domain events (e.g. `CustomerUpdatedEvent` → drop `customer:{id}` cache key).

---

## 12. Common Mistakes

| Mistake                                                          | Fix                                                              |
|------------------------------------------------------------------|------------------------------------------------------------------|
| Singular collection (`/api/customer`)                            | Plural: `/api/customers`                                         |
| `200 OK` with `{ "error": "..." }`                               | Status code is the contract; use 4xx with ProblemDetails         |
| `POST /api/getCustomer`                                          | `GET /api/customers/{id}`                                        |
| Returning a bare array from a list endpoint                      | `{ items, page, pageSize, total }`                               |
| Different error formats across endpoints                         | Always ProblemDetails                                            |
| `204 No Content` with a body                                     | If you have a body, use `200 OK`                                 |
| `PUT` that updates only some fields                              | Use `PATCH`                                                      |
| `Authorization: <jwt>` (missing Bearer)                          | `Authorization: Bearer <jwt>`                                    |
| Hardcoded API URLs in clients                                    | Use config/env                                                   |
| Mixing camelCase and snake_case in JSON                          | Always camelCase                                                 |
| Returning DB entity directly                                     | Always a DTO                                                     |
| `400` for "not found"                                            | `404`                                                            |
| `500` for validation                                             | `400` with field errors                                          |
