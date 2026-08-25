namespace MyCondo.Application.Features.Finance.FinancialStatements.Notes;

/// <summary>Extracts a generic (Label, Amount, IsUnattributed) row set from whichever typed row-list is
/// populated on a <see cref="FinancialStatementNote"/> (exactly one of the six is non-null per note —
/// see the note's doc comment). Shared by CSV export (Task 8's
/// <c>FinancialStatementsCsvExportMapper</c>) and the PDF renderer (<c>FinancialStatementsPdfRenderer</c>)
/// so both export surfaces render the same detail rows the same way, from one place, instead of each
/// re-implementing the per-note-type switch.</summary>
public static class FinancialStatementNoteRowExtractor
{
    public static IReadOnlyList<(string Label, decimal Amount, bool IsUnattributed)> GetRows(FinancialStatementNote note)
    {
        if (note.CashAndBankRows is { } cashRows)
        {
            return cashRows.Select(r => (BuildCashLabel(r), r.Balance, false)).ToList();
        }

        if (note.FixedDepositRows is { } fixedDepositRows)
        {
            return fixedDepositRows.Select(r => (
                r.CertificateNumber ?? r.BankName ?? "Unattributed Fixed Deposit Activity",
                r.CarryingBalance,
                r.IsUnattributed)).ToList();
        }

        if (note.FlatBalanceRows is { } flatBalanceRows)
        {
            return flatBalanceRows.Select(r => (BuildFlatLabel(r), r.Balance, r.IsUnattributed)).ToList();
        }

        if (note.PayableRows is { } payableRows)
        {
            return payableRows.Select(r => (
                r.Description ?? r.Payee ?? "Unattributed Payable Activity",
                r.OutstandingAmount,
                r.IsUnattributed)).ToList();
        }

        if (note.InterestIncomeRows is { } interestIncomeRows)
        {
            return interestIncomeRows.Select(r => (
                r.CertificateNumber ?? r.BankName ?? "Unattributed Interest Activity",
                r.InterestRecognized,
                r.IsUnattributed)).ToList();
        }

        if (note.OperatingExpenseCategoryRows is { } operatingExpenseCategoryRows)
        {
            return operatingExpenseCategoryRows.Select(r => (r.CategoryName, r.Amount, r.IsUnattributed)).ToList();
        }

        return [];
    }

    private static string BuildCashLabel(CashAndBankNoteRow row)
    {
        string label = row.Name;
        if (!string.IsNullOrWhiteSpace(row.BankName))
        {
            label = $"{row.BankName} — {label}";
        }

        if (!string.IsNullOrWhiteSpace(row.MaskedAccountNumber))
        {
            label = $"{label} ({row.MaskedAccountNumber})";
        }

        if (row.IsUnlinkedGlAccount)
        {
            label = $"{label} — Unlinked GL Account";
        }

        return label;
    }

    private static string BuildFlatLabel(FlatBalanceNoteRow row) =>
        row.FlatNumber is not null
            ? (row.BuildingName is not null ? $"{row.BuildingName} — {row.FlatNumber}" : row.FlatNumber)
            : "Unattributed Flat Activity";
}
