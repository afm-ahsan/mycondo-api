using Mediator;
using MyCondo.Application.Features.Payments.DTOs;
using MyCondo.Domain.Common;

namespace MyCondo.Application.Features.Payments.Queries.GetLedgerEntriesForAccount;

public sealed record GetLedgerEntriesForAccountQuery(
    Guid FlatId,
    DateOnly? FromDate,
    DateOnly? ToDate,
    string? ReferenceType,
    int Page,
    int PageSize
) : IRequest<PagedResult<LedgerEntryDto>>;
