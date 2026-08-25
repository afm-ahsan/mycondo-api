using System.Globalization;
using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Finance.FinancialStatements.Notes;
using MyCondo.Application.Features.Finance.FinancialStatements.Queries.GetFinancialPositionNotes;
using MyCondo.Application.Features.Finance.FinancialStatements.Queries.GetIncomeExpenditureNotes;
using MyCondo.Application.Features.Finance.FinancialStatements.Queries.GetIncomeExpenditureStatement;
using MyCondo.Application.Features.Finance.FinancialStatements.Queries.GetStatementOfFinancialPosition;
using MyCondo.Domain.Features.Finance.Funds;
using MyCondo.Domain.Features.Tenancy;

namespace MyCondo.Application.Features.Finance.FinancialStatements.Queries.ExportFinancialStatements;

/// <summary>Composes the same four GL-backed queries the interactive Financial Statements UI calls
/// (Statement of Financial Position, Income &amp; Expenditure, both Notes-to-Accounts queries) into one
/// export — never a second aggregation path, never a recomputed total. CSV goes through the shared
/// <see cref="IReportExportService"/> (the single-flat-table document model fits a long-form CSV
/// perfectly); PDF goes through the bespoke <see cref="IFinancialStatementsPdfRenderer"/>, since a
/// composite multi-section accounting document does not fit that same flat-table model. Notes from both
/// statements are merged and ordered by the durable <see cref="FinancialStatementNoteKey"/> (not
/// declaration order in either response) so "Note 1, Note 2, ..." numbering stays stable across
/// requests, independent of which schedules happen to have data for a given tenant/period.</summary>
public sealed class ExportFinancialStatementsQueryHandler(
    ISender sender,
    IReportExportService csvExportService,
    IFinancialStatementsPdfRenderer pdfRenderer,
    IFundRepository funds,
    ITenantRepository tenants,
    ICurrentUserProvider currentUser
) : IRequestHandler<ExportFinancialStatementsQuery, ReportExportResult>
{
    public async ValueTask<ReportExportResult> Handle(ExportFinancialStatementsQuery query, CancellationToken cancellationToken)
    {
        StatementOfFinancialPositionDto position = await sender.Send(
            new GetStatementOfFinancialPositionQuery(query.EndDate, query.FundId), cancellationToken);
        IncomeExpenditureStatementDto incomeExpenditure = await sender.Send(
            new GetIncomeExpenditureStatementQuery(query.StartDate, query.EndDate, query.FundId), cancellationToken);
        FinancialStatementNotesDto positionNotes = await sender.Send(
            new GetFinancialPositionNotesQuery(query.EndDate, query.FundId, null), cancellationToken);
        FinancialStatementNotesDto periodNotes = await sender.Send(
            new GetIncomeExpenditureNotesQuery(query.StartDate, query.EndDate, query.FundId, null), cancellationToken);

        // Deterministic, durable-key ordering (Task 8 spec §11-15) — never the order either Notes
        // response happens to return.
        List<FinancialStatementNote> notes = positionNotes.Notes
            .Concat(periodNotes.Notes)
            .OrderBy(n => n.Key)
            .ToList();

        string fundLabel = await ResolveFundLabelAsync(query.FundId, cancellationToken);
        string fileName =
            $"financial-statements-{query.StartDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}" +
            $"-to-{query.EndDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}";

        if (query.Format == ReportExportFormat.Csv)
        {
            ReportExportDocument document =
                FinancialStatementsCsvExportMapper.ToExportDocument(position, incomeExpenditure, notes, fundLabel);
            return await csvExportService.ExportAsync(document, ReportExportFormat.Csv, fileName, cancellationToken);
        }

        string organizationName = await ResolveOrganizationNameAsync(cancellationToken);
        byte[] pdfBytes = await pdfRenderer.RenderAsync(
            organizationName, fundLabel, position, incomeExpenditure, notes, cancellationToken);

        return new ReportExportResult(new MemoryStream(pdfBytes), "application/pdf", $"{fileName}.pdf");
    }

    /// <summary>Organization name for the PDF header — reuses the already-registered
    /// <see cref="ITenantRepository"/> (no new architecture) rather than hardcoding a tenant-specific
    /// name. Falls back to a generic label only if the tenant record cannot be resolved (should not
    /// happen for an authenticated caller, but this must never throw a report export into a 500 over a
    /// display string).</summary>
    private async Task<string> ResolveOrganizationNameAsync(CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            return "CondoBD";
        }

        Tenant? tenant = await tenants.GetByIdAsync(tenantId, cancellationToken);
        return tenant?.Name ?? "CondoBD";
    }

    /// <summary>Fund label for the document header/metadata. <see cref="IFundRepository"/> is already
    /// registered and used elsewhere in Finance (e.g. Fixed Deposit detail) — reusing it here is not new
    /// architecture. Falls back to a generic label if the fund cannot be resolved, per the Task 8 spec's
    /// explicit escape hatch, rather than adding any further lookup.</summary>
    private async Task<string> ResolveFundLabelAsync(Guid? fundId, CancellationToken cancellationToken)
    {
        if (fundId is not Guid id)
        {
            return "All Funds";
        }

        Fund? fund = await funds.GetByIdAsync(new FundId(id), cancellationToken);
        return fund?.Name ?? "Selected Fund";
    }
}
