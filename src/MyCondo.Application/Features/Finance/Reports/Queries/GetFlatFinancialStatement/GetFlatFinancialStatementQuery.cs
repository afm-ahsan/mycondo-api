using Mediator;
using MyCondo.Application.Features.Finance.Reports.Contracts;

namespace MyCondo.Application.Features.Finance.Reports.Queries.GetFlatFinancialStatement;

public sealed record FlatFinancialStatementLineDto(
    Guid LedgerEntryId,
    Guid PostingId,
    DateOnly BusinessDate,
    string Description,
    string? ReferenceType,
    Guid? ReferenceId,
    string Direction,
    decimal Amount,
    decimal RunningBalance);

public sealed record FlatFinancialStatementReportDto(
    FinanceReportMetadataDto Metadata,
    Guid FlatId,
    string FlatNumber,
    decimal OpeningBalance,
    decimal PeriodDebitTotal,
    decimal PeriodCreditTotal,
    decimal ClosingBalance,
    IReadOnlyList<FlatFinancialStatementLineDto> Lines,
    int Page,
    int PageSize,
    long Total);

/// <summary>Board/admin-facing per-flat financial statement — any flat in the tenant, gated by
/// <c>finance.report.view</c> (not resource-scoped to the caller's own flat; that's the separate
/// Resident Financial Statement report). Wraps <c>ILedgerEntryRepository.SearchForFlatChronologicalAsync</c>
/// (the same oldest-first, running-balance-ready browse the Resident Financial Statement report uses)
/// with opening/closing balance and period debit/credit totals framing on top.</summary>
public sealed record GetFlatFinancialStatementQuery(
    Guid FlatId,
    DateOnly? FromDate,
    DateOnly? ToDate,
    int Page,
    int PageSize
) : IRequest<FlatFinancialStatementReportDto>;
