using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Payments.DTOs;
using MyCondo.Application.Features.Payments.Mappings;
using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Payments.Ledger;
using MyCondo.Domain.Features.Property.Flats;

namespace MyCondo.Application.Features.Payments.Queries.GetLedgerEntriesForAccount;

public sealed class GetLedgerEntriesForAccountQueryHandler(
    ILedgerEntryRepository ledgerEntries,
    ICurrentUserProvider currentUser
) : IRequestHandler<GetLedgerEntriesForAccountQuery, PagedResult<LedgerEntryDto>>
{
    public async ValueTask<PagedResult<LedgerEntryDto>> Handle(
        GetLedgerEntriesForAccountQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        FlatId flatId = new(query.FlatId);
        PagedResult<LedgerEntryWithReference> result = await ledgerEntries.SearchForFlatAsync(
            tenantId, flatId, query.FromDate, query.ToDate, query.ReferenceType, query.Page, query.PageSize, cancellationToken);

        List<LedgerEntryDto> items = result.Items.Select(e => e.ToDto()).ToList();

        return new PagedResult<LedgerEntryDto>(items, result.Page, result.PageSize, result.Total);
    }
}
