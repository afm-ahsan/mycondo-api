using Mediator;
using MyCondo.Application.Features.Payments.DTOs;
using MyCondo.Domain.Common;

namespace MyCondo.Application.Features.Payments.Queries.GetLedgerEntriesForAccount;

public sealed record GetLedgerEntriesForAccountQuery(
    Guid FlatId,
    int Page,
    int PageSize
) : IRequest<PagedResult<LedgerEntryDto>>;
