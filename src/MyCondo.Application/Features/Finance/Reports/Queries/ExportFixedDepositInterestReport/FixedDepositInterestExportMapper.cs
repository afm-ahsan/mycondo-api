using System.Globalization;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Finance.Reports.Queries.GetFixedDepositInterestReport;

namespace MyCondo.Application.Features.Finance.Reports.Queries.ExportFixedDepositInterestReport;

public static class FixedDepositInterestExportMapper
{
    public static ReportExportDocument ToExportDocument(FixedDepositInterestReportDto report)
    {
        List<(string Label, string Value)> metadataLines =
        [
            ("From", report.Metadata.FromDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? string.Empty),
            ("To", report.Metadata.ToDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? string.Empty),
            ("Scope", report.Metadata.ScopeSummary),
            ("Currency", report.Metadata.Currency),
            ("Basis", report.Metadata.AccountingBasis),
        ];

        List<ReportExportColumn> columns =
        [
            new("Certificate Number"),
            new("Accrued (Gross)", IsNumeric: true),
            new("Received (Gross)", IsNumeric: true),
            new("Received (Deduction)", IsNumeric: true),
            new("Received (Net)", IsNumeric: true),
        ];

        List<IReadOnlyList<string>> rows = report.ByFixedDeposit
            .Select(line => (IReadOnlyList<string>)
            [
                line.CertificateNumber,
                FormatAmount(line.AccruedGrossForPeriod),
                FormatAmount(line.ReceivedGrossForPeriod),
                FormatAmount(line.ReceivedDeductionForPeriod),
                FormatAmount(line.ReceivedNetForPeriod),
            ])
            .ToList();

        List<(string Label, string Value)> totals =
        [
            ("Accrued (Gross) for Period", FormatAmount(report.AccruedGrossForPeriod)),
            ("Ledger Interest Income for Period", FormatAmount(report.LedgerInterestIncomeForPeriod)),
            ("Reconciled", report.IsReconciled ? "Yes" : "No"),
            ("Received (Gross) for Period", FormatAmount(report.ReceivedGrossForPeriod)),
            ("Received (Deduction) for Period", FormatAmount(report.ReceivedDeductionForPeriod)),
            ("Received (Net) for Period", FormatAmount(report.ReceivedNetForPeriod)),
            ("Outstanding Accrued (Not Received) as of To Date", FormatAmount(report.OutstandingAccruedNotReceivedAsOfToDate)),
        ];

        return new ReportExportDocument("Fixed Deposit Interest", metadataLines, columns, rows, totals);
    }

    private static string FormatAmount(decimal value) => value.ToString("N2", CultureInfo.InvariantCulture);
}
