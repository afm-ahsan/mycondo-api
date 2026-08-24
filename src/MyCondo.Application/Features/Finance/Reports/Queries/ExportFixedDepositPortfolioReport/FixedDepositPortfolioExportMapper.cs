using System.Globalization;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Finance.Reports.Queries.GetFixedDepositPortfolioReport;

namespace MyCondo.Application.Features.Finance.Reports.Queries.ExportFixedDepositPortfolioReport;

public static class FixedDepositPortfolioExportMapper
{
    public static ReportExportDocument ToExportDocument(FixedDepositPortfolioReportDto report)
    {
        List<(string Label, string Value)> metadataLines =
        [
            ("As Of", report.Metadata.AsOfDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? string.Empty),
            ("Scope", report.Metadata.ScopeSummary),
            ("Currency", report.Metadata.Currency),
            ("Basis", report.Metadata.AccountingBasis),
        ];

        List<ReportExportColumn> columns =
        [
            new("Certificate Number"),
            new("Bank"),
            new("Branch"),
            new("Principal", IsNumeric: true),
            new("Rate (%)", IsNumeric: true),
            new("Start Date"),
            new("Maturity Date"),
            new("Status"),
            new("Accrued Interest", IsNumeric: true),
            new("Received Interest", IsNumeric: true),
            new("Outstanding Interest", IsNumeric: true),
        ];

        List<IReadOnlyList<string>> rows = report.Lines
            .Select(line => (IReadOnlyList<string>)
            [
                line.CertificateNumber,
                line.BankName,
                line.BranchName ?? string.Empty,
                FormatAmount(line.Principal),
                FormatRate(line.InterestRatePercent),
                line.StartDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                line.MaturityDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                line.Status,
                FormatAmount(line.TotalAccruedInterest),
                FormatAmount(line.TotalReceivedInterest),
                FormatAmount(line.OutstandingAccruedInterest),
            ])
            .ToList();

        List<(string Label, string Value)> totals =
        [
            ("Total Principal", FormatAmount(report.TotalPrincipal)),
            ("Total Outstanding Accrued Interest", FormatAmount(report.TotalOutstandingAccruedInterest)),
            ("Active Count", report.ActiveCount.ToString(CultureInfo.InvariantCulture)),
        ];

        return new ReportExportDocument("Fixed Deposit Portfolio", metadataLines, columns, rows, totals);
    }

    private static string FormatAmount(decimal value) => value.ToString("N2", CultureInfo.InvariantCulture);

    private static string FormatRate(decimal value) => value.ToString("N2", CultureInfo.InvariantCulture);
}
