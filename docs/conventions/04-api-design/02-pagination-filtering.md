# Pagination, Filtering, Sorting

Every list endpoint follows the same shape so frontend code can build generic table components against it.

---

## 1. Standard Query Shape

```
GET /api/<resource>?search=<text>&<filter>=<value>&sort=<field>&page=<n>&pageSize=<n>
```

### Reserved query parameters

| Parameter   | Type     | Default | Max | Purpose                              |
|-------------|----------|---------|-----|--------------------------------------|
| `search`    | string   | —       | —   | Full-text-ish keyword search         |
| `page`      | int      | 1       | —   | 1-based page number                  |
| `pageSize`  | int      | 20      | 100 | Items per page                       |
| `sort`      | string   | per-resource | — | `field` ASC; `-field` DESC; comma-separated for multi-sort |

### Rules

- **`page` is 1-based.** Cleaner UX. The backend converts to OFFSET internally.
- **`pageSize` capped at 100** (FluentValidation enforces).
- **`sort` syntax**: `name` (asc), `-name` (desc). Multi-field: `-createdAtUtc,name`.
- **Resource-specific filters** use the field name as the query key: `?status=Active&customerId=...`.

---

## 2. Response Shape

```json
{
  "items": [
    { "id": "...", "name": "Alice", "email": "alice@example.com" }
  ],
  "page": 1,
  "pageSize": 20,
  "total": 47
}
```

### TypeScript shape

```ts
// src/types/api.types.ts
export type PagedResult<T> = {
  items: T[];
  page: number;
  pageSize: number;
  total: number;
};
```

### C# shape

```csharp
public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    long Total)
{
    public int TotalPages => (int)Math.Ceiling(Total / (double)PageSize);
    public bool HasNext => Page < TotalPages;
    public bool HasPrevious => Page > 1;
}
```

---

## 3. Backend Implementation (Read Side, Dapper)

```csharp
public async Task<PagedResult<CustomerSummaryDto>> SearchAsync(
    SearchCustomersQuery q,
    CancellationToken ct)
{
    var sortClause = ParseSort(q.Sort, allowed: ["name", "createdAtUtc"]);

    var sql = $$"""
        SELECT id, name, email, status, created_at_utc AS CreatedAtUtc
        FROM app.customer
        WHERE deleted_at_utc IS NULL
          AND (@Search IS NULL OR name ILIKE '%' || @Search || '%' OR email ILIKE '%' || @Search || '%')
          AND (@Status IS NULL OR status = @Status)
        ORDER BY {{sortClause}}
        OFFSET @Offset LIMIT @Limit;
        """;

    const string countSql = """
        SELECT COUNT(*)
        FROM app.customer
        WHERE deleted_at_utc IS NULL
          AND (@Search IS NULL OR name ILIKE '%' || @Search || '%' OR email ILIKE '%' || @Search || '%')
          AND (@Status IS NULL OR status = @Status);
        """;

    var args = new { q.Search, q.Status, Offset = (q.Page - 1) * q.PageSize, Limit = q.PageSize };
    await using var conn = await dataSource.OpenConnectionAsync(ct);

    var items = (await conn.QueryAsync<CustomerSummaryDto>(
        new CommandDefinition(sql, args, cancellationToken: ct))).ToList();
    var total = await conn.ExecuteScalarAsync<long>(
        new CommandDefinition(countSql, args, cancellationToken: ct));

    return new PagedResult<CustomerSummaryDto>(items, q.Page, q.PageSize, total);
}

private static string ParseSort(string? sort, IReadOnlyCollection<string> allowed)
{
    if (string.IsNullOrWhiteSpace(sort))
        return "created_at_utc DESC";

    var parts = sort.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    var clauses = new List<string>();
    foreach (var part in parts)
    {
        var desc = part.StartsWith('-');
        var field = (desc ? part[1..] : part).ToLowerInvariant();
        if (!allowed.Contains(field)) continue;
        clauses.Add($"{ToSnakeCase(field)} {(desc ? "DESC" : "ASC")}");
    }
    return clauses.Count > 0 ? string.Join(", ", clauses) : "created_at_utc DESC";
}
```

### Rules

- **Sort field whitelist.** Never inject the raw `sort` parameter into SQL — only allowed fields.
- **Search uses `ILIKE` with wildcards.** For high-traffic searches, switch to `pg_trgm` GIN index.
- **Count and items in two queries.** EF Core's `CountAsync` + `ToListAsync` work too, but Dapper gives you control.
- **Always order by `created_at_utc DESC`** when no sort is requested — newest first is the typical default.

---

## 4. Backend Implementation (Write Side, EF Core)

When pagination is needed on the EF side (e.g. for aggregate-loading admin pages):

```csharp
public async Task<PagedResult<Customer>> SearchAsync(SearchCustomersQuery q, CancellationToken ct)
{
    var query = db.Customers
        .AsNoTracking()
        .Where(c => c.DeletedAtUtc == null);

    if (!string.IsNullOrWhiteSpace(q.Search))
        query = query.Where(c =>
            EF.Functions.ILike(c.Name, $"%{q.Search}%") ||
            EF.Functions.ILike(c.Email.Value, $"%{q.Search}%"));

    var total = await query.LongCountAsync(ct);

    var items = await query
        .OrderByDescending(c => c.CreatedAtUtc)
        .Skip((q.Page - 1) * q.PageSize)
        .Take(q.PageSize)
        .ToListAsync(ct);

    return new PagedResult<Customer>(items, q.Page, q.PageSize, total);
}
```

### Rules

- **`AsNoTracking()`** for read-only paths.
- **Apply `Where` filters before `Count` and `Skip/Take`** to keep the query plan tight.
- **Prefer Dapper for hot read paths.** EF is fine for moderate read traffic.

---

## 5. Validation

```csharp
public sealed class SearchCustomersQueryValidator : AbstractValidator<SearchCustomersQuery>
{
    public SearchCustomersQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.Search).MaximumLength(200);
        RuleFor(x => x.Sort).MaximumLength(100);
        RuleFor(x => x.Status).Must(BeValidStatus!).When(x => x.Status is not null);
    }

    private static bool BeValidStatus(string status) =>
        status is "Active" or "Inactive";
}
```

### Rules

- **Always validate paging params.** A `pageSize=10000` request is a denial-of-service vector.
- **Whitelist enum values** via `.Must(...)`.
- **Validate the sort string is reasonable length.**

---

## 6. Frontend — Building Queries

```tsx
import { useSearchParams } from 'react-router-dom';

export function CustomersListPage() {
  const [params, setParams] = useSearchParams();
  const query = {
    search: params.get('search') ?? undefined,
    status: params.get('status') ?? undefined,
    sort: params.get('sort') ?? '-createdAtUtc',
    page: Number(params.get('page')) || 1,
    pageSize: Number(params.get('pageSize')) || 20,
  };

  const { data, isFetching } = useSearchCustomersQuery(query);

  function update(next: Partial<typeof query>) {
    const merged = { ...query, ...next };
    const newParams = new URLSearchParams();
    Object.entries(merged).forEach(([k, v]) => {
      if (v !== undefined && v !== '' && v !== 1 /* default page */) newParams.set(k, String(v));
    });
    setParams(newParams);
  }

  // ... render table, sort headers call update({ sort: '-name' }), etc.
}
```

### Rules

- **Filters in the URL**, not in component state. Refresh-safe, sharable.
- **Default values omitted** from the URL for clean links (`/customers` cleaner than `/customers?page=1&pageSize=20&sort=-createdAtUtc`).
- **`isFetching` is the right loading flag for paginated tables** — `isLoading` is only true on the first request.

---

## 7. Cursor-Based Pagination (When Needed)

For very large datasets or infinite-scroll UIs, switch to cursor pagination:

```
GET /api/customers?cursor=<opaque>&pageSize=20
```

Response:

```json
{
  "items": [...],
  "nextCursor": "eyJjcmVhdGVkQXRVdGMiOi...",
  "pageSize": 20
}
```

The cursor encodes `(createdAtUtc, id)` of the last item. Server returns items where `(createdAtUtc, id) < cursor`.

### Rules

- **Use cursor pagination for >100K rows** — offset pagination scans every skipped row.
- **Cursors are opaque strings.** Clients pass them through; never parse.
- **Stable sort field required.** Add a tiebreaker (`id`) to handle ties.

---

## 8. Complex Filters via POST

When filters get complex (multi-field range, nested conditions), accept a JSON body via `POST`:

```
POST /api/customers/search HTTP/1.1
Content-Type: application/json

{
  "filter": {
    "status": ["Active", "OnHold"],
    "createdBetween": { "from": "2026-01-01", "to": "2026-12-31" },
    "tags": { "any": ["vip", "enterprise"] }
  },
  "sort": "-createdAtUtc",
  "page": 1,
  "pageSize": 20
}
```

### Rules

- **Use POST sparingly** — break GET semantics; not cacheable.
- **Same response shape** as GET list endpoints.
- **Document why** a query needs POST (URL length limit, filter complexity).

---

## 9. Common Mistakes

| Mistake                                                       | Fix                                                              |
|---------------------------------------------------------------|------------------------------------------------------------------|
| 0-based `page`                                                | 1-based — UX-friendlier                                          |
| `pageSize` uncapped                                           | Cap at 100 in validator                                          |
| Sort field interpolated into raw SQL                          | Whitelist + map                                                  |
| Returning bare array `[{...}, {...}]`                         | Wrap in `{ items, page, pageSize, total }`                       |
| Filters in component state, lost on refresh                   | URL via `useSearchParams`                                        |
| `total` calculated wrong (filter not applied)                 | Apply filter to both items query and count query                 |
| Cursor pagination for small datasets                          | Offset pagination is fine; cursor adds complexity                |
| Sort default missing                                          | Always order by `created_at_utc DESC` if no sort                 |
| `OFFSET 1000000` queries                                      | Switch to cursor pagination                                      |
