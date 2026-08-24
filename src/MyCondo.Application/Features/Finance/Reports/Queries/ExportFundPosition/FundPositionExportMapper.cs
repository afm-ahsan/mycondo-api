using System.Globalization;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Finance.Reports.Queries.GetFundPosition;

namespace MyCondo.Application.Features.Finance.Reports.Queries.ExportFundPosition;

public static class FundPositionExportMapper
{
    public static ReportExportDocument ToExportDocument(FundPositionReportDto report)
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
            new("Code"),
            new("Fund"),
            new("Balance", IsNumeric: true),
        ];

        List<IReadOnlyList<string>> rows = report.Funds
            .Select(fund => (IReadOnlyList<string>)
            [
                fund.Code,
                fund.Name,
                FormatAmount(fund.Balance),
            ])
            .ToList();

        List<(string Label, string Value)> totals =
        [
            ("Total Balance", FormatAmount(report.TotalBalance)),
        ];

        return new ReportExportDocument("Fund Position", metadataLines, columns, rows, totals);
    }

    private static string FormatAmount(decimal value) => value.ToString("N2", CultureInfo.InvariantCulture);
}
