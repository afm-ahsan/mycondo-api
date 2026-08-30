using Mediator;
using MyCondo.Application.Common.Abstractions;

namespace MyCondo.Application.Features.Finance.FinancialStatements.Queries.ExportFinancialStatements;

/// <summary>PDF/CSV export of the Financial Statements — CondoBD Finance Phase 2A Task 8. Carries the
/// same already-resolved [<see cref="StartDate"/>, <see cref="EndDate"/>] window and
/// <see cref="FundId"/> the interactive Financial Statements UI is currently showing; comparison export
/// is explicitly out of scope for this task (see the Task 8 spec §3/§10) — this is always a
/// current-period, non-comparative document.</summary>
public sealed record ExportFinancialStatementsQuery(
    DateOnly StartDate, DateOnly EndDate, Guid? FundId, ReportExportFormat Format) : IRequest<ReportExportResult>, ILifecycleReadOperation;
