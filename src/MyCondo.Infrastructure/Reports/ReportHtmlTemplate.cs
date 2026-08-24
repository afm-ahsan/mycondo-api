using System.Globalization;
using System.Net;
using System.Text;
using MyCondo.Application.Common;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Domain.Abstractions;

namespace MyCondo.Infrastructure.Reports;

/// <summary>Builds the single HTML template every PDF-exported report renders through. Kept as a
/// plain string builder (no Razor view engine) since the content is simple tabular data — one
/// template serves every report, so a future report only needs a <see cref="ReportExportDocument"/>
/// mapper, never its own markup.</summary>
public sealed class ReportHtmlTemplate(IClock clock)
{
    public string Render(ReportExportDocument document)
    {
        StringBuilder sb = new();
        sb.Append("<!doctype html><html><head><meta charset=\"utf-8\"><style>")
            .Append("""
                body { font-family: Arial, Helvetica, sans-serif; font-size: 11px; color: #1a1a1a; margin: 24px; }
                h1 { font-size: 18px; margin: 0 0 12px; }
                table.meta { margin-bottom: 16px; border-collapse: collapse; }
                table.meta td { padding: 2px 8px 2px 0; vertical-align: top; }
                table.meta td.label { color: #555; white-space: nowrap; }
                table.data { width: 100%; border-collapse: collapse; }
                table.data th, table.data td { border: 1px solid #ccc; padding: 4px 8px; text-align: left; }
                table.data th { background: #f2f2f2; }
                table.data td.numeric, table.data th.numeric { text-align: right; }
                table.totals { margin-top: 12px; border-collapse: collapse; break-inside: avoid; page-break-inside: avoid; }
                table.totals td { padding: 2px 8px 2px 0; font-weight: bold; }
                """)
            .Append("</style></head><body>");

        sb.Append("<h1>").Append(Encode(document.Title)).Append("</h1>");

        sb.Append("<table class=\"meta\">");
        foreach ((string label, string value) in document.MetadataLines)
        {
            sb.Append("<tr><td class=\"label\">").Append(Encode(label)).Append("</td><td>")
                .Append(Encode(value)).Append("</td></tr>");
        }

        sb.Append("<tr><td class=\"label\">Generated At</td><td>")
            .Append(Encode(DhakaTimeZone.ToLocal(clock.UtcNow)
                .ToString("yyyy-MM-dd HH:mm 'Asia/Dhaka'", CultureInfo.InvariantCulture)))
            .Append("</td></tr>");
        sb.Append("</table>");

        sb.Append("<table class=\"data\"><thead><tr>");
        foreach (ReportExportColumn column in document.Columns)
        {
            sb.Append("<th class=\"").Append(column.IsNumeric ? "numeric" : "").Append("\">")
                .Append(Encode(column.Header)).Append("</th>");
        }

        sb.Append("</tr></thead><tbody>");
        foreach (IReadOnlyList<string> row in document.Rows)
        {
            sb.Append("<tr>");
            for (int i = 0; i < row.Count; i++)
            {
                bool numeric = i < document.Columns.Count && document.Columns[i].IsNumeric;
                sb.Append("<td class=\"").Append(numeric ? "numeric" : "").Append("\">")
                    .Append(Encode(row[i])).Append("</td>");
            }

            sb.Append("</tr>");
        }

        sb.Append("</tbody></table>");

        if (document.Totals is { Count: > 0 })
        {
            sb.Append("<table class=\"totals\">");
            foreach ((string label, string value) in document.Totals)
            {
                sb.Append("<tr><td>").Append(Encode(label)).Append("</td><td>")
                    .Append(Encode(value)).Append("</td></tr>");
            }

            sb.Append("</table>");
        }

        sb.Append("</body></html>");
        return sb.ToString();
    }

    private static string Encode(string value) => WebUtility.HtmlEncode(value);
}
