using MyCondo.Application.Features.Finance.FinancialStatements.Notes;
using MyCondo.Application.Features.Finance.FinancialStatements.Queries.GetIncomeExpenditureStatement;
using MyCondo.Application.Features.Finance.FinancialStatements.Queries.GetStatementOfFinancialPosition;

namespace MyCondo.Application.Common.Abstractions;

/// <summary>Renders the composite Financial Statements PDF (Statement of Financial Position, Income
/// &amp; Expenditure Statement, Notes to Accounts) — CondoBD Finance Phase 2A Task 8. Deliberately
/// separate from <see cref="IReportExportService"/>/<see cref="ReportExportDocument"/>: that model is a
/// single flat table and cannot express grouped sections, indented account lines, subtotals, several
/// Note blocks each with their own reconciliation strip, or an integrity banner. Takes the
/// already-fetched statement DTOs — implementations must never re-query — so the export renders exactly
/// the same GL-derived figures the interactive Financial Statements UI shows, with no second aggregation
/// path.</summary>
public interface IFinancialStatementsPdfRenderer
{
    Task<byte[]> RenderAsync(
        string organizationName,
        string fundLabel,
        StatementOfFinancialPositionDto financialPosition,
        IncomeExpenditureStatementDto incomeExpenditure,
        IReadOnlyList<FinancialStatementNote> notes,
        CancellationToken cancellationToken);
}
