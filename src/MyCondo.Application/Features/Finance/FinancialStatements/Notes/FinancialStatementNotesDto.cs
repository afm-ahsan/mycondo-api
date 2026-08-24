using MyCondo.Application.Features.Finance.Reports.Contracts;

namespace MyCondo.Application.Features.Finance.FinancialStatements.Notes;

/// <summary>The supporting schedules for one Financial Statement rendering — CondoBD Finance Phase 2A
/// Task 5. Returned as a set rather than one endpoint per schedule because a statement is normally
/// rendered with all of its notes at once and they share a single date/fund window; a caller that wants
/// only one schedule filters with the query's note-key parameter rather than calling a different
/// endpoint. Detail rows are capped per schedule (see
/// <see cref="FinancialStatementNote.IsDetailTruncated"/>) so the payload stays bounded on a large
/// estate while every total remains complete.</summary>
public sealed record FinancialStatementNotesDto(
    FinanceReportMetadataDto Metadata,
    Guid? FundId,
    IReadOnlyList<FinancialStatementNote> Notes);
