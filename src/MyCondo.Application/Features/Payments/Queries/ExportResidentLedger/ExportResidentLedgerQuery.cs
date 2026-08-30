using Mediator;
using MyCondo.Application.Common.Abstractions;

namespace MyCondo.Application.Features.Payments.Queries.ExportResidentLedger;

/// <summary>No Page/PageSize here, unlike <see cref="GetLedgerEntriesForAccount.GetLedgerEntriesForAccountQuery"/>
/// — export always returns the FULL matching result set for the flat's resident account and filters
/// (see <see cref="ExportResidentLedgerQueryHandler"/>), never just one page of the on-screen browse.</summary>
public sealed record ExportResidentLedgerQuery(
    Guid FlatId,
    DateOnly? FromDate,
    DateOnly? ToDate,
    string? ReferenceType,
    ReportExportFormat Format
) : IRequest<ReportExportResult>, ILifecycleReadOperation;
