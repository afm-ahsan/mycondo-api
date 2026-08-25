using System.Globalization;
using System.Net;
using System.Text;
using MyCondo.Application.Common;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Finance.FinancialStatements;
using MyCondo.Application.Features.Finance.FinancialStatements.Notes;
using MyCondo.Application.Features.Finance.FinancialStatements.Queries.GetIncomeExpenditureStatement;
using MyCondo.Application.Features.Finance.FinancialStatements.Queries.GetStatementOfFinancialPosition;
using MyCondo.Domain.Abstractions;

namespace MyCondo.Infrastructure.Reports;

/// <summary>Renders the CondoBD Finance Phase 2A Task 8 Financial Statements PDF — one composite
/// document (Statement of Financial Position, Income &amp; Expenditure Statement, Notes to Accounts)
/// built entirely from the already-fetched, GL-backed DTOs the interactive Financial Statements UI also
/// renders; this class never re-queries and never recomputes a total. Reuses the shared
/// <see cref="PlaywrightPdfRenderer"/> singleton for the actual HTML→PDF step (its fixed A4/margin page
/// options — shared by all 36 exported reports — are untouched here), but builds its own bespoke HTML
/// rather than going through <see cref="ReportHtmlTemplate"/>: that template's single flat-table model
/// cannot express grouped sections, indented account lines, subtotals, several Note blocks each with
/// their own reconciliation strip, or an integrity banner. The base CSS conventions (Arial/Helvetica,
/// bordered data tables, right-aligned numeric columns, break-inside: avoid on totals/section/Note
/// blocks) mirror <see cref="ReportHtmlTemplate"/> for visual consistency with the rest of the product.</summary>
public sealed class FinancialStatementsPdfRenderer(PlaywrightPdfRenderer pdfRenderer, IClock clock)
    : IFinancialStatementsPdfRenderer
{
    public async Task<byte[]> RenderAsync(
        string organizationName,
        string fundLabel,
        StatementOfFinancialPositionDto financialPosition,
        IncomeExpenditureStatementDto incomeExpenditure,
        IReadOnlyList<FinancialStatementNote> notes,
        CancellationToken cancellationToken)
    {
        string html = BuildHtml(organizationName, fundLabel, financialPosition, incomeExpenditure, notes);
        return await pdfRenderer.RenderPdfAsync(html, cancellationToken);
    }

    private string BuildHtml(
        string organizationName,
        string fundLabel,
        StatementOfFinancialPositionDto position,
        IncomeExpenditureStatementDto incomeExpenditure,
        IReadOnlyList<FinancialStatementNote> notes)
    {
        StringBuilder sb = new();
        sb.Append("<!doctype html><html><head><meta charset=\"utf-8\"><style>").Append(Css()).Append("</style></head><body>");

        AppendHeader(sb, organizationName, fundLabel, position, incomeExpenditure);
        AppendFinancialPosition(sb, position);
        AppendIncomeExpenditure(sb, incomeExpenditure);
        AppendNotes(sb, notes);

        sb.Append("</body></html>");
        return sb.ToString();
    }

    private static string Css() => """
        body { font-family: Arial, Helvetica, sans-serif; font-size: 11px; color: #1a1a1a; margin: 24px; }
        h1 { font-size: 18px; margin: 4px 0; }
        h2 { font-size: 14px; margin: 0 0 2px; }
        h3 { font-size: 12px; margin: 0 0 4px; }
        .header { margin-bottom: 20px; }
        .header .org { font-size: 14px; font-weight: bold; }
        .header .meta { color: #444; margin-top: 2px; }
        .status { display: inline-block; margin-top: 6px; padding: 2px 8px; border: 1px solid #999; color: #555; font-size: 10px; }
        .section { margin-bottom: 20px; break-inside: avoid; page-break-inside: avoid; }
        table.statement { width: 100%; border-collapse: collapse; margin-top: 4px; }
        table.statement td { padding: 2px 4px; }
        table.statement td.amount { text-align: right; white-space: nowrap; }
        table.statement td.indent { padding-left: 20px; }
        table.statement tr.group-row td { font-weight: bold; padding-top: 6px; }
        table.statement tr.total-row td { border-top: 2px solid #333; border-bottom: 2px solid #333; font-weight: bold; padding-top: 4px; padding-bottom: 4px; }
        .banner { margin-top: 8px; padding: 6px 10px; border-radius: 3px; font-weight: bold; display: inline-block; }
        .banner.balanced { background: #e6f4ea; color: #1e7e34; }
        .banner.warning { background: #fdecea; color: #b02a20; }
        .warnings { margin-top: 8px; padding: 6px 10px; background: #fff8e1; color: #7a5b00; font-size: 10px; }
        .warnings ul { margin: 4px 0 0; padding-left: 18px; }
        .note-block { margin-bottom: 16px; break-inside: avoid; page-break-inside: avoid; }
        table.data { width: 100%; border-collapse: collapse; margin-top: 4px; }
        table.data th, table.data td { border: 1px solid #ccc; padding: 3px 6px; text-align: left; }
        table.data th { background: #f2f2f2; }
        table.data td.numeric, table.data th.numeric { text-align: right; }
        table.reconciliation { margin-top: 4px; border-collapse: collapse; }
        table.reconciliation td { padding: 2px 8px 2px 0; }
        table.reconciliation td.label { color: #555; }
        .truncation-note { font-style: italic; color: #555; margin-top: 4px; font-size: 10px; }
        """;

    private void AppendHeader(
        StringBuilder sb, string organizationName, string fundLabel,
        StatementOfFinancialPositionDto position, IncomeExpenditureStatementDto incomeExpenditure)
    {
        string period =
            $"{incomeExpenditure.Metadata.FromDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)} – " +
            $"{incomeExpenditure.Metadata.ToDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}";
        string asOf = position.Metadata.AsOfDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? string.Empty;
        string generatedAt = DhakaTimeZone.ToLocal(clock.UtcNow)
            .ToString("yyyy-MM-dd HH:mm 'Asia/Dhaka'", CultureInfo.InvariantCulture);

        sb.Append("<div class=\"header\">");
        sb.Append("<div class=\"org\">").Append(Encode(organizationName)).Append("</div>");
        sb.Append("<h1>FINANCIAL STATEMENTS</h1>");
        sb.Append("<div class=\"meta\">Reporting Period: ").Append(Encode(period)).Append("</div>");
        sb.Append("<div class=\"meta\">Financial Position As Of: ").Append(Encode(asOf)).Append("</div>");
        sb.Append("<div class=\"meta\">Fund: ").Append(Encode(fundLabel)).Append("</div>");
        sb.Append("<div class=\"meta\">Currency: ").Append(Encode(position.Metadata.Currency)).Append("</div>");
        sb.Append("<div class=\"meta\">Generated: ").Append(Encode(generatedAt)).Append("</div>");
        sb.Append("<div class=\"status\">Unaudited Management Report</div>");
        sb.Append("</div>");
    }

    private static void AppendFinancialPosition(StringBuilder sb, StatementOfFinancialPositionDto position)
    {
        sb.Append("<div class=\"section\">");
        sb.Append("<h2>1. STATEMENT OF FINANCIAL POSITION</h2>");
        sb.Append("<div class=\"meta\">As of ")
            .Append(Encode(position.Metadata.AsOfDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? string.Empty))
            .Append("</div>");

        sb.Append("<table class=\"statement\">");
        sb.Append("<tr><td colspan=\"2\"><strong>ASSETS</strong></td></tr>");
        AppendGroups(sb, position.Assets.Groups);
        sb.Append("<tr class=\"total-row\"><td>TOTAL ASSETS</td><td class=\"amount\">").Append(FormatCurrency(position.TotalAssets)).Append("</td></tr>");

        sb.Append("<tr><td colspan=\"2\">&nbsp;</td></tr>");
        sb.Append("<tr><td colspan=\"2\"><strong>LIABILITIES</strong></td></tr>");
        AppendGroups(sb, position.Liabilities.Groups);
        sb.Append("<tr class=\"total-row\"><td>TOTAL LIABILITIES</td><td class=\"amount\">").Append(FormatCurrency(position.TotalLiabilities)).Append("</td></tr>");

        sb.Append("<tr><td colspan=\"2\">&nbsp;</td></tr>");
        sb.Append("<tr><td colspan=\"2\"><strong>FUNDS / ACCUMULATED SURPLUS</strong></td></tr>");
        AppendGroups(sb, position.Funds.Groups);
        sb.Append("<tr><td class=\"indent\">Cumulative Surplus / (Deficit)</td><td class=\"amount\">")
            .Append(FormatCurrency(position.CumulativeSurplusDeficit)).Append("</td></tr>");
        sb.Append("<tr class=\"total-row\"><td>TOTAL FUNDS</td><td class=\"amount\">").Append(FormatCurrency(position.TotalFunds)).Append("</td></tr>");

        sb.Append("<tr><td colspan=\"2\">&nbsp;</td></tr>");
        sb.Append("<tr class=\"total-row\"><td>TOTAL LIABILITIES &amp; FUNDS</td><td class=\"amount\">")
            .Append(FormatCurrency(position.TotalLiabilitiesAndFunds)).Append("</td></tr>");
        sb.Append("</table>");

        sb.Append(position.IsBalanced
            ? "<div class=\"banner balanced\">Balanced</div>"
            : $"<div class=\"banner warning\">Financial Integrity Warning — Difference: {FormatCurrency(position.Difference)}</div>");

        List<(string Code, string Name, decimal Amount)> positionWarnings =
            position.UnmappedAccountWarnings.Select(w => (w.Code, w.Name, w.Amount)).ToList();
        AppendUnmappedWarnings(sb, positionWarnings);

        sb.Append("</div>");
    }

    private static void AppendIncomeExpenditure(StringBuilder sb, IncomeExpenditureStatementDto incomeExpenditure)
    {
        sb.Append("<div class=\"section\">");
        sb.Append("<h2>2. INCOME &amp; EXPENDITURE STATEMENT</h2>");
        string period =
            $"{incomeExpenditure.Metadata.FromDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)} – " +
            $"{incomeExpenditure.Metadata.ToDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}";
        sb.Append("<div class=\"meta\">").Append(Encode(period)).Append("</div>");

        sb.Append("<table class=\"statement\">");
        sb.Append("<tr><td colspan=\"2\"><strong>INCOME</strong></td></tr>");
        AppendGroups(sb, incomeExpenditure.Income.Groups);
        sb.Append("<tr class=\"total-row\"><td>TOTAL INCOME</td><td class=\"amount\">").Append(FormatCurrency(incomeExpenditure.TotalIncome)).Append("</td></tr>");

        sb.Append("<tr><td colspan=\"2\">&nbsp;</td></tr>");
        sb.Append("<tr><td colspan=\"2\"><strong>EXPENDITURE</strong></td></tr>");
        AppendGroups(sb, incomeExpenditure.Expenditure.Groups);
        sb.Append("<tr class=\"total-row\"><td>TOTAL EXPENDITURE</td><td class=\"amount\">").Append(FormatCurrency(incomeExpenditure.TotalExpenditure)).Append("</td></tr>");

        sb.Append("<tr><td colspan=\"2\">&nbsp;</td></tr>");
        sb.Append("<tr class=\"total-row\"><td>SURPLUS / (DEFICIT)</td><td class=\"amount\">").Append(FormatCurrency(incomeExpenditure.SurplusDeficit)).Append("</td></tr>");
        sb.Append("</table>");

        List<(string Code, string Name, decimal Amount)> ieWarnings =
            incomeExpenditure.UnmappedAccountWarnings.Select(w => (w.Code, w.Name, w.Amount)).ToList();
        AppendUnmappedWarnings(sb, ieWarnings);

        sb.Append("</div>");
    }

    private static void AppendGroups(
        StringBuilder sb, IReadOnlyList<StatementOfFinancialPositionGroupDto> groups)
    {
        foreach (StatementOfFinancialPositionGroupDto group in groups)
        {
            AppendGroupRow(sb, group.Group.ToDisplayName(), group.Amount);
            foreach (StatementOfFinancialPositionAccountLineDto account in group.Accounts)
            {
                AppendAccountRow(sb, account.Code, account.Name, account.Amount);
            }
        }
    }

    private static void AppendGroups(StringBuilder sb, IReadOnlyList<IncomeExpenditureStatementGroupDto> groups)
    {
        foreach (IncomeExpenditureStatementGroupDto group in groups)
        {
            AppendGroupRow(sb, group.Group.ToDisplayName(), group.Amount);
            foreach (IncomeExpenditureAccountLineDto account in group.Accounts)
            {
                AppendAccountRow(sb, account.Code, account.Name, account.Amount);
            }
        }
    }

    private static void AppendGroupRow(StringBuilder sb, string label, decimal amount) =>
        sb.Append("<tr class=\"group-row\"><td>").Append(Encode(label)).Append("</td><td class=\"amount\">")
            .Append(FormatCurrency(amount)).Append("</td></tr>");

    private static void AppendAccountRow(StringBuilder sb, string code, string name, decimal amount) =>
        sb.Append("<tr><td class=\"indent\">").Append(Encode($"{code} — {name}")).Append("</td><td class=\"amount\">")
            .Append(FormatCurrency(amount)).Append("</td></tr>");

    private static void AppendUnmappedWarnings(StringBuilder sb, List<(string Code, string Name, decimal Amount)> warnings)
    {
        if (warnings.Count == 0)
        {
            return;
        }

        sb.Append("<div class=\"warnings\"><strong>Classification Attention Required</strong> — ")
            .Append(warnings.Count).Append(warnings.Count == 1 ? " account is" : " accounts are")
            .Append(" using fallback Financial Statement classification:<ul>");
        foreach ((string code, string name, decimal amount) in warnings)
        {
            sb.Append("<li>").Append(Encode(code)).Append(" — ").Append(Encode(name))
                .Append(" (").Append(FormatCurrency(amount)).Append(")</li>");
        }

        sb.Append("</ul></div>");
    }

    private static void AppendNotes(StringBuilder sb, IReadOnlyList<FinancialStatementNote> notes)
    {
        sb.Append("<div class=\"section\"><h2>3. NOTES TO ACCOUNTS</h2>");
        int number = 1;
        foreach (FinancialStatementNote note in notes)
        {
            AppendNote(sb, number, note);
            number++;
        }

        sb.Append("</div>");
    }

    private static void AppendNote(StringBuilder sb, int number, FinancialStatementNote note)
    {
        sb.Append("<div class=\"note-block\">");
        sb.Append("<h3>Note ").Append(number).Append(" — ").Append(Encode(note.Title)).Append("</h3>");

        IReadOnlyList<(string Label, decimal Amount, bool IsUnattributed)> rows = FinancialStatementNoteRowExtractor.GetRows(note);
        sb.Append("<table class=\"data\"><thead><tr><th>Description</th><th class=\"numeric\">Amount</th></tr></thead><tbody>");
        foreach ((string label, decimal amount, bool isUnattributed) in rows)
        {
            string displayLabel = isUnattributed ? $"{label} (Unattributed)" : label;
            sb.Append("<tr><td>").Append(Encode(displayLabel)).Append("</td><td class=\"numeric\">")
                .Append(FormatCurrency(amount)).Append("</td></tr>");
        }

        sb.Append("</tbody></table>");

        sb.Append("<table class=\"reconciliation\">");
        sb.Append("<tr><td class=\"label\">Supporting Schedule</td><td>").Append(FormatCurrency(note.ScheduleBalance)).Append("</td></tr>");
        sb.Append("<tr><td class=\"label\">General Ledger</td><td>").Append(FormatCurrency(note.GlBalance)).Append("</td></tr>");
        sb.Append("<tr><td class=\"label\">Difference</td><td>").Append(FormatCurrency(note.Difference)).Append("</td></tr>");
        sb.Append("<tr><td class=\"label\">Status</td><td>")
            .Append(note.IsReconciled ? "Reconciled" : "Reconciliation Difference").Append("</td></tr>");
        sb.Append("</table>");

        if (note.IsDetailTruncated)
        {
            sb.Append("<div class=\"truncation-note\">Showing a subset of ").Append(note.TotalRowCount)
                .Append(" total rows — the schedule display is truncated but the totals above remain complete.</div>");
        }

        if (note.Warnings.Count > 0)
        {
            sb.Append("<div class=\"warnings\"><ul>");
            foreach (string warning in note.Warnings)
            {
                sb.Append("<li>").Append(Encode(warning)).Append("</li>");
            }

            sb.Append("</ul></div>");
        }

        sb.Append("</div>");
    }

    private static string FormatCurrency(decimal amount)
    {
        decimal abs = Math.Abs(amount);
        string formatted = "৳" + abs.ToString("N2", CultureInfo.InvariantCulture);
        return amount < 0 ? $"({formatted})" : formatted;
    }

    private static string Encode(string value) => WebUtility.HtmlEncode(value);
}
