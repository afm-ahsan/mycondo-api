using Microsoft.EntityFrameworkCore;
using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Billing.Invoices;
using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Domain.Features.Property.Flats;

namespace MyCondo.Infrastructure.Persistence.Repositories;

public sealed class InvoiceRepository(MyCondoDbContext db) : IInvoiceRepository
{
    public Task<Invoice?> GetByIdAsync(InvoiceId id, CancellationToken cancellationToken) =>
        db.Set<Invoice>().FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

    public async Task<PagedResult<Invoice>> SearchAsync(
        Guid tenantId,
        BuildingId? buildingId,
        FlatId? flatId,
        InvoiceStatus? status,
        InvoiceSource? source,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        IQueryable<Invoice> query = db.Set<Invoice>()
            .AsNoTracking()
            .Where(i => i.TenantId == tenantId);

        if (buildingId is not null)
        {
            query = query.Where(i => i.BuildingId == buildingId);
        }

        if (flatId is not null)
        {
            query = query.Where(i => i.FlatId == flatId);
        }

        if (status is not null)
        {
            query = query.Where(i => i.Status == status);
        }

        if (source is not null)
        {
            query = query.Where(i => i.Source == source);
        }

        long total = await query.LongCountAsync(cancellationToken);

        List<Invoice> items = await query
            .OrderByDescending(i => i.InvoiceDate)
            .ThenByDescending(i => i.InvoiceNumber)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Invoice>(items, page, pageSize, total);
    }

    public Task<bool> ExistsForFlatAndPeriodAsync(
        Guid tenantId, FlatId flatId, DateOnly periodStart, DateOnly periodEnd, InvoiceSource source,
        CancellationToken cancellationToken) =>
        db.Set<Invoice>()
            .AsNoTracking()
            .AnyAsync(i =>
                i.TenantId == tenantId && i.FlatId == flatId && i.PeriodStart == periodStart &&
                i.PeriodEnd == periodEnd && i.Source == source,
                cancellationToken);

    /// <summary>Locks rows via <c>FOR UPDATE</c> per financial-engine.md invariant 5 — tracked (not
    /// AsNoTracking), since the caller mutates <see cref="Invoice.AmountPaid"/>/<see cref="Invoice.Status"/>
    /// via <see cref="Invoice.ApplyPayment"/> in the same unit of work.</summary>
    public async Task<IReadOnlyList<Invoice>> GetOutstandingForFlatForUpdateAsync(
        Guid tenantId, FlatId flatId, CancellationToken cancellationToken) =>
        await db.Set<Invoice>()
            .FromSqlInterpolated($"""
                SELECT * FROM billing.invoices
                WHERE tenant_id = {tenantId}
                  AND flat_id = {flatId.Value}
                  AND status IN ('Issued', 'PartiallyPaid')
                ORDER BY due_date ASC, invoice_date ASC, invoice_number ASC
                FOR UPDATE
                """)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<InvoiceLine>> GetLinesForInvoiceAsync(InvoiceId invoiceId, CancellationToken cancellationToken) =>
        await db.Set<InvoiceLine>().AsNoTracking().Where(l => l.InvoiceId == invoiceId).ToListAsync(cancellationToken);

    public void Add(Invoice invoice) => db.Set<Invoice>().Add(invoice);

    public void AddLines(IEnumerable<InvoiceLine> lines) => db.Set<InvoiceLine>().AddRange(lines);
}
