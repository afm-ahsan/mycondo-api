using System.Globalization;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Finance.Integrity.DTOs;

namespace MyCondo.Application.Features.Finance.Integrity.Queries.ExportFinancialIntegrityDashboard;

/// <summary>This dashboard is a live diagnostic/reconciliation-check view, not a traditional ledger
/// table, so it's rendered as a "Category / Check / Description / Count / Status" table — one row per
/// check, mirroring exactly what <c>FinancialIntegrityDashboardPage</c> shows on screen — rather than
/// forcing it into a financial debit/credit shape.</summary>
public static class FinancialIntegrityDashboardExportMapper
{
    private const string DataIntegrityCategory = "Data Integrity";
    private const string OperationalBacklogCategory = "Operational Backlog";
    private const string HealthyStatus = "Healthy";
    private const string AttentionStatus = "Attention";

    public static ReportExportDocument ToExportDocument(FinancialIntegrityDashboardDto dashboard)
    {
        List<(string Label, string Value)> metadataLines =
        [
            ("Overall Status", dashboard.IsHealthy ? HealthyStatus : "Attention required"),
        ];

        List<ReportExportColumn> columns =
        [
            new("Category"),
            new("Check"),
            new("Description"),
            new("Count", IsNumeric: true),
            new("Status"),
        ];

        List<IReadOnlyList<string>> rows =
        [
            Row(DataIntegrityCategory, "Unbalanced postings",
                "Ledger postings whose debits and credits do not sum to zero.",
                dashboard.UnbalancedPostingsCount),
            Row(DataIntegrityCategory, "Duplicate logical postings",
                "Postings sharing the same source reference — a duplicate financial effect.",
                dashboard.DuplicateLogicalPostingsCount),
            Row(DataIntegrityCategory, "Closed-period violations",
                "Ledger entries recorded after their accounting period was closed.",
                dashboard.ClosedPeriodViolationsCount),
            Row(OperationalBacklogCategory, "Stale unreconciled bank items",
                "Bank statement lines still unmatched more than 45 days after the statement date.",
                dashboard.StaleUnreconciledBankItemsCount),
            Row(OperationalBacklogCategory, "Stale unreceived interest accruals",
                "Fixed Deposit interest accrued more than 45 days ago with no matching receipt.",
                dashboard.StaleUnreceivedInterestAccrualsCount),
        ];

        return new ReportExportDocument("Financial Integrity Dashboard", metadataLines, columns, rows);
    }

    private static IReadOnlyList<string> Row(string category, string check, string description, long count) =>
    [
        category,
        check,
        description,
        count.ToString(CultureInfo.InvariantCulture),
        count == 0 ? HealthyStatus : AttentionStatus,
    ];
}
