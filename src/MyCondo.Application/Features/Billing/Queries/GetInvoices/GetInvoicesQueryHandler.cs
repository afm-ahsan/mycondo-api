using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Billing.DTOs;
using MyCondo.Application.Features.Billing.Mappings;
using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Billing.Invoices;
using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Domain.Features.Property.Flats;

namespace MyCondo.Application.Features.Billing.Queries.GetInvoices;

public sealed class GetInvoicesQueryHandler(
    IInvoiceRepository invoices,
    ICurrentUserProvider currentUser
) : IRequestHandler<GetInvoicesQuery, PagedResult<InvoiceDto>>
{
    public async ValueTask<PagedResult<InvoiceDto>> Handle(GetInvoicesQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        BuildingId? buildingId = query.BuildingId is Guid rawBuildingId ? new BuildingId(rawBuildingId) : null;
        FlatId? flatId = query.FlatId is Guid rawFlatId ? new FlatId(rawFlatId) : null;
        InvoiceStatus? status = query.Status is null ? null : Enum.Parse<InvoiceStatus>(query.Status);
        InvoiceSource? source = query.Source is null ? null : Enum.Parse<InvoiceSource>(query.Source);

        PagedResult<Invoice> result = await invoices.SearchAsync(
            tenantId, buildingId, flatId, status, source, query.Page, query.PageSize, cancellationToken);

        List<InvoiceDto> items = result.Items.Select(i => i.ToDto()).ToList();

        return new PagedResult<InvoiceDto>(items, result.Page, result.PageSize, result.Total);
    }
}
