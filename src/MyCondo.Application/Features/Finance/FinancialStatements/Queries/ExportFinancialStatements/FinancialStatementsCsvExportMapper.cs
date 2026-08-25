using System.Globalization;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Finance.FinancialStatements.Notes;
using MyCondo.Application.Features.Finance.FinancialStatements.Queries.GetIncomeExpenditureStatement;
using MyCondo.Application.Features.Finance.FinancialStatements.Queries.GetStatementOfFinancialPosition;

namespace MyCondo.Application.Features.Finance.FinancialStatements.Queries.ExportFinancialStatements;

/// <summary>Maps the Financial Statements' four composed DTOs to the long-form, machine-readable
/// <see cref="ReportExportDocument"/> the Task 8 spec's CSV design calls for (§16-17) — one row per
/// account line, one group-subtotal row, one section-total row per statement, then one summary row per
/// Note followed by that Note's own detail rows. Never a visual dump of the PDF: every total already
/// came from the GL-backed query DTOs, this only reshapes them into the flat table
/// <see cref="IReportExportService"/> renders.</summary>
public static class FinancialStatementsCsvExportMapper
{
    public static ReportExportDocument ToExportDocument(
        StatementOfFinancialPositionDto position,
        IncomeExpenditureStatementDto incomeExpenditure,
        IReadOnlyList<FinancialStatementNote> notes,
        string fundLabel)
    {
        List<(string Label, string Value)> metadataLines =
        [
            ("Reporting Period",
                $"{incomeExpenditure.Metadata.FromDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)} " +
                $"to {incomeExpenditure.Metadata.ToDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}"),
            ("Financial Position As Of",
                position.Metadata.AsOfDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? string.Empty),
            ("Fund", fundLabel),
            ("Currency", position.Metadata.Currency),
        ];

        List<ReportExportColumn> columns =
        [
            new("Statement"),
            new("Section"),
            new("Group"),
            new("AccountOrLine"),
            new("Amount", IsNumeric: true),
            new("NoteKey"),
            new("NoteTitle"),
            new("ReconciliationStatus"),
            new("GlBalance", IsNumeric: true),
            new("ScheduleBalance", IsNumeric: true),
            new("Difference", IsNumeric: true),
        ];

        List<IReadOnlyList<string>> rows = [];
        AppendFinancialPositionRows(rows, position);
        AppendIncomeExpenditureRows(rows, incomeExpenditure);
        AppendNoteRows(rows, notes);

        List<(string Label, string Value)> totals =
        [
            ("Total Assets", FormatAmount(position.TotalAssets)),
            ("Total Liabilities", FormatAmount(position.TotalLiabilities)),
            ("Total Funds", FormatAmount(position.TotalFunds)),
            ("Total Liabilities & Funds", FormatAmount(position.TotalLiabilitiesAndFunds)),
            ("Financial Position Difference", FormatAmount(position.Difference)),
            ("Financial Position Status", position.IsBalanced ? "Balanced" : "Financial Integrity Warning"),
            ("Total Income", FormatAmount(incomeExpenditure.TotalIncome)),
            ("Total Expenditure", FormatAmount(incomeExpenditure.TotalExpenditure)),
            ("Surplus / (Deficit)", FormatAmount(incomeExpenditure.SurplusDeficit)),
        ];

        return new ReportExportDocument("Financial Statements", metadataLines, columns, rows, totals);
    }

    private static void AppendFinancialPositionRows(List<IReadOnlyList<string>> rows, StatementOfFinancialPositionDto position)
    {
        AppendPositionSection(rows, "Assets", position.Assets, "TOTAL ASSETS", position.TotalAssets, includeCumulativeSurplus: false, position);
        AppendPositionSection(rows, "Liabilities", position.Liabilities, "TOTAL LIABILITIES", position.TotalLiabilities, includeCumulativeSurplus: false, position);
        AppendPositionSection(rows, "Funds", position.Funds, "TOTAL FUNDS", position.TotalFunds, includeCumulativeSurplus: true, position);
    }

    private static void AppendPositionSection(
        List<IReadOnlyList<string>> rows, string sectionName, StatementOfFinancialPositionSectionDto section,
        string totalLabel, decimal sectionTotal, bool includeCumulativeSurplus, StatementOfFinancialPositionDto position)
    {
        foreach (StatementOfFinancialPositionGroupDto group in section.Groups)
        {
            string groupName = group.Group.ToDisplayName();
            foreach (StatementOfFinancialPositionAccountLineDto account in group.Accounts)
            {
                rows.Add(Row("Financial Position", sectionName, groupName, $"{account.Code} - {account.Name}", account.Amount));
            }

            rows.Add(Row("Financial Position", sectionName, groupName, $"{groupName} (Subtotal)", group.Amount));
        }

        if (includeCumulativeSurplus)
        {
            rows.Add(Row("Financial Position", sectionName, string.Empty, "Cumulative Surplus / (Deficit)", position.CumulativeSurplusDeficit));
        }

        rows.Add(Row("Financial Position", sectionName, string.Empty, totalLabel, sectionTotal));
    }

    private static void AppendIncomeExpenditureRows(List<IReadOnlyList<string>> rows, IncomeExpenditureStatementDto incomeExpenditure)
    {
        AppendIncomeExpenditureSection(rows, "Income", incomeExpenditure.Income, "TOTAL INCOME", incomeExpenditure.TotalIncome);
        AppendIncomeExpenditureSection(rows, "Expenditure", incomeExpenditure.Expenditure, "TOTAL EXPENDITURE", incomeExpenditure.TotalExpenditure);
        rows.Add(Row("Income & Expenditure", string.Empty, string.Empty, "SURPLUS / (DEFICIT)", incomeExpenditure.SurplusDeficit));
    }

    private static void AppendIncomeExpenditureSection(
        List<IReadOnlyList<string>> rows, string sectionName, IncomeExpenditureStatementSectionDto section,
        string totalLabel, decimal sectionTotal)
    {
        foreach (IncomeExpenditureStatementGroupDto group in section.Groups)
        {
            string groupName = group.Group.ToDisplayName();
            foreach (IncomeExpenditureAccountLineDto account in group.Accounts)
            {
                rows.Add(Row("Income & Expenditure", sectionName, groupName, $"{account.Code} - {account.Name}", account.Amount));
            }

            rows.Add(Row("Income & Expenditure", sectionName, groupName, $"{groupName} (Subtotal)", group.Amount));
        }

        rows.Add(Row("Income & Expenditure", sectionName, string.Empty, totalLabel, sectionTotal));
    }

    private static void AppendNoteRows(List<IReadOnlyList<string>> rows, IReadOnlyList<FinancialStatementNote> notes)
    {
        foreach (FinancialStatementNote note in notes)
        {
            string status = note.IsReconciled ? "Reconciled" : "Reconciliation Difference";
            rows.Add(Row(
                "Notes to Accounts", string.Empty, string.Empty, note.Title, note.ScheduleBalance,
                note.Key.ToString(), note.Title, status, note.GlBalance, note.ScheduleBalance, note.Difference));

            foreach ((string label, decimal amount, bool isUnattributed) in FinancialStatementNoteRowExtractor.GetRows(note))
            {
                string detailLabel = isUnattributed ? $"{label} (Unattributed)" : label;
                rows.Add(Row(
                    "Notes to Accounts", string.Empty, string.Empty, detailLabel, amount,
                    note.Key.ToString(), note.Title, null, null, null, null));
            }
        }
    }

    private static IReadOnlyList<string> Row(
        string statement, string section, string group, string accountOrLine, decimal amount,
        string? noteKey = null, string? noteTitle = null, string? reconciliationStatus = null,
        decimal? glBalance = null, decimal? scheduleBalance = null, decimal? difference = null) =>
    [
        statement,
        section,
        group,
        accountOrLine,
        FormatAmount(amount),
        noteKey ?? string.Empty,
        noteTitle ?? string.Empty,
        reconciliationStatus ?? string.Empty,
        glBalance is decimal g ? FormatAmount(g) : string.Empty,
        scheduleBalance is decimal s ? FormatAmount(s) : string.Empty,
        difference is decimal d ? FormatAmount(d) : string.Empty,
    ];

    private static string FormatAmount(decimal value) => value.ToString("N2", CultureInfo.InvariantCulture);
}
