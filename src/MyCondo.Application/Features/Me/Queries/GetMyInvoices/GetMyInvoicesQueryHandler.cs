using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Billing.DTOs;
using MyCondo.Application.Features.Billing.Mappings;
using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Billing.Invoices;
using MyCondo.Domain.Features.Property.Flats;

namespace MyCondo.Application.Features.Me.Queries.GetMyInvoices;

/// <summary>
/// Self-service: invoices for the caller's own owned/occupied Flats only. Gated by a dedicated
/// invoice.view.own permission rather than the existing broad billing.invoice.view (which also
/// unlocks the admin invoice-listing endpoint, covering every Flat in a Building/Tenant) — reusing
/// billing.invoice.view here would let a FlatOwner/Tenant role holder call that admin endpoint too,
/// exposing every other flat's invoices. See the Phase-3 spec's "add .own/.self permissions only
/// where current broad permissions would expose too much."
/// </summary>
public sealed class GetMyInvoicesQueryHandler(
    IFlatAccessAuthorizer flatAccessAuthorizer,
    IInvoiceRepository invoices,
    ICurrentUserProvider currentUser
) : IRequestHandler<GetMyInvoicesQuery, List<InvoiceDto>>
{
    private const string InvoiceViewOwnPermission = "invoice.view.own";
    private const int MaxInvoicesPerFlat = 200;

    public async ValueTask<List<InvoiceDto>> Handle(GetMyInvoicesQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId || currentUser.UserId is not Guid userId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        List<FlatRelationship> relationships =
            await flatAccessAuthorizer.GetActiveRelationshipsAsync(tenantId, userId, cancellationToken);

        List<Guid> accessibleFlatIds = relationships
            .Where(r => currentUser.HasPermissionForBuilding(InvoiceViewOwnPermission, r.BuildingId))
            .Select(r => r.FlatId)
            .Distinct()
            .ToList();

        List<InvoiceDto> result = [];
        foreach (Guid flatId in accessibleFlatIds)
        {
            PagedResult<Invoice> page = await invoices.SearchAsync(
                tenantId, buildingId: null, new FlatId(flatId), status: null, source: null,
                page: 1, pageSize: MaxInvoicesPerFlat, cancellationToken);

            result.AddRange(page.Items.Select(i => i.ToDto()));
        }

        return result;
    }
}
