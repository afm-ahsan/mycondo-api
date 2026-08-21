using Mediator;
using MyCondo.Application.Features.Finance.Reports.Contracts;

namespace MyCondo.Application.Features.Finance.Reports.Queries.GetResidentFinancialStatementReport;

public sealed record ResidentStatementLineDto(
    Guid EntryId,
    Guid PostingId,
    DateOnly BusinessDate,
    string Description,
    string? ReferenceType,
    Guid? ReferenceId,
    string Direction,
    decimal Amount,
    decimal RunningBalance);

public sealed record ResidentFinancialStatementReportDto(
    FinanceReportMetadataDto Metadata,
    Guid FlatId,
    string FlatNumber,
    decimal OpeningBalance,
    IReadOnlyList<ResidentStatementLineDto> Lines,
    decimal ClosingBalance,
    int Page,
    int PageSize,
    long Total);

/// <summary>A resident/flat's full ledger-derived statement (charges, payments, running balance).
/// <see cref="FlatId"/> is whichever flat the caller is asking about — the handler enforces the
/// Resident Privacy rule (self-service callers may only request their own flat) rather than this query
/// type restricting it structurally.</summary>
public sealed record GetResidentFinancialStatementReportQuery(
    Guid FlatId,
    DateOnly? FromDate,
    DateOnly? ToDate,
    int Page,
    int PageSize
) : IRequest<ResidentFinancialStatementReportDto>;
