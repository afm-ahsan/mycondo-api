using Microsoft.EntityFrameworkCore;
using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Platform.OrganizationSubscriptions;
using MyCondo.Domain.Features.Platform.SubscriptionInvoices;

namespace MyCondo.Infrastructure.Persistence.Repositories;

public sealed class SubscriptionInvoiceRepository(MyCondoDbContext db) : ISubscriptionInvoiceRepository
{
    public Task<SubscriptionInvoice?> GetByIdAsync(SubscriptionInvoiceId id, CancellationToken cancellationToken) =>
        db.Set<SubscriptionInvoice>().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<SubscriptionInvoice>> GetForOrganizationSubscriptionAsync(
        OrganizationSubscriptionId organizationSubscriptionId, CancellationToken cancellationToken) =>
        await db.Set<SubscriptionInvoice>()
            .Where(x => x.OrganizationSubscriptionId == organizationSubscriptionId)
            .OrderByDescending(x => x.BillingPeriodStart)
            .ToListAsync(cancellationToken);

    public async Task<PagedResult<SubscriptionInvoice>> SearchAsync(
        int page,
        int pageSize,
        Guid? tenantId,
        SubscriptionInvoiceStatus? status,
        bool? overdueOnly,
        DateOnly? dueDateFrom,
        DateOnly? dueDateTo,
        DateOnly today,
        CancellationToken cancellationToken)
    {
        IQueryable<SubscriptionInvoice> query = db.Set<SubscriptionInvoice>().AsNoTracking();

        if (tenantId is not null)
        {
            query = query.Where(x => x.TenantId == tenantId.Value);
        }

        if (status is not null)
        {
            query = query.Where(x => x.Status == status.Value);
        }

        if (overdueOnly == true)
        {
            query = query.Where(x =>
                x.Status == SubscriptionInvoiceStatus.Issued && x.OutstandingAmount > 0 && x.DueDate < today);
        }

        if (dueDateFrom is not null)
        {
            query = query.Where(x => x.DueDate >= dueDateFrom.Value);
        }

        if (dueDateTo is not null)
        {
            query = query.Where(x => x.DueDate <= dueDateTo.Value);
        }

        long total = await query.LongCountAsync(cancellationToken);

        List<SubscriptionInvoice> items = await query
            .OrderBy(x => x.DueDate)
            .ThenByDescending(x => x.IssueDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<SubscriptionInvoice>(items, page, pageSize, total);
    }

    public async Task<IReadOnlyList<SubscriptionInvoice>> GetOutstandingAsync(
        Guid? tenantId, CancellationToken cancellationToken)
    {
        IQueryable<SubscriptionInvoice> query = db.Set<SubscriptionInvoice>().AsNoTracking()
            .Where(x => x.Status == SubscriptionInvoiceStatus.Issued && x.OutstandingAmount > 0);

        if (tenantId is not null)
        {
            query = query.Where(x => x.TenantId == tenantId.Value);
        }

        return await query.OrderBy(x => x.DueDate).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SubscriptionInvoice>> GetIssuedInRangeAsync(
        Guid? tenantId, DateOnly? issueDateFrom, DateOnly? issueDateTo, CancellationToken cancellationToken)
    {
        IQueryable<SubscriptionInvoice> query = db.Set<SubscriptionInvoice>().AsNoTracking();

        if (tenantId is not null)
        {
            query = query.Where(x => x.TenantId == tenantId.Value);
        }

        if (issueDateFrom is not null)
        {
            query = query.Where(x => x.IssueDate >= issueDateFrom.Value);
        }

        if (issueDateTo is not null)
        {
            query = query.Where(x => x.IssueDate <= issueDateTo.Value);
        }

        return await query.OrderByDescending(x => x.IssueDate).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SubscriptionInvoiceLine>> GetLinesAsync(
        SubscriptionInvoiceId subscriptionInvoiceId, CancellationToken cancellationToken) =>
        await db.Set<SubscriptionInvoiceLine>().AsNoTracking()
            .Where(x => x.SubscriptionInvoiceId == subscriptionInvoiceId)
            .OrderBy(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

    public void Add(SubscriptionInvoice invoice) => db.Set<SubscriptionInvoice>().Add(invoice);

    public void AddLines(IEnumerable<SubscriptionInvoiceLine> lines) => db.Set<SubscriptionInvoiceLine>().AddRange(lines);
}
