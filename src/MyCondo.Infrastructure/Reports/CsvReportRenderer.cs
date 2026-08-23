using System.Globalization;
using System.Text;
using MyCondo.Application.Common;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Domain.Abstractions;

namespace MyCondo.Infrastructure.Reports;

/// <summary>Renders a <see cref="ReportExportDocument"/> to CSV. UTF-8 BOM is prepended so Excel
/// opens Bangla/multi-byte content correctly — the same requirement the client-side CSV helper it
/// replaces (mycondo-web src/lib/reports/exportCsv.ts) already documented.</summary>
public sealed class CsvReportRenderer(IClock clock)
{
    public byte[] Render(ReportExportDocument document)
    {
        StringBuilder sb = new();

        sb.Append(EscapeCell(document.Title)).Append("\r\n");
        foreach ((string label, string value) in document.MetadataLines)
        {
            sb.Append(EscapeCell(label)).Append(',').Append(EscapeCell(value)).Append("\r\n");
        }

        sb.Append(EscapeCell("Generated At")).Append(',')
            .Append(EscapeCell(DhakaTimeZone.ToLocal(clock.UtcNow)
                .ToString("yyyy-MM-dd HH:mm 'Asia/Dhaka'", CultureInfo.InvariantCulture)))
            .Append("\r\n");

        sb.Append("\r\n");

        sb.Append(string.Join(',', document.Columns.Select(c => EscapeCell(c.Header)))).Append("\r\n");
        foreach (IReadOnlyList<string> row in document.Rows)
        {
            sb.Append(string.Join(',', row.Select(EscapeCell))).Append("\r\n");
        }

        if (document.Totals is { Count: > 0 })
        {
            sb.Append("\r\n");
            foreach ((string label, string value) in document.Totals)
            {
                sb.Append(EscapeCell(label)).Append(',').Append(EscapeCell(value)).Append("\r\n");
            }
        }

        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
    }

    private static string EscapeCell(string value) =>
        value.IndexOfAny(['"', ',', '\n', '\r']) >= 0
            ? $"\"{value.Replace("\"", "\"\"")}\""
            : value;
}
