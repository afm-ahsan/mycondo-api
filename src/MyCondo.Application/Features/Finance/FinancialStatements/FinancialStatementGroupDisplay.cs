using MyCondo.Domain.Features.Finance.ChartOfAccounts;

namespace MyCondo.Application.Features.Finance.FinancialStatements;

/// <summary>Organization-agnostic display label for a <see cref="FinancialStatementGroup"/> — shared by
/// both Task 8 export surfaces (CSV mapper and PDF renderer) so a group's rendered name is defined in
/// exactly one place. Never derived from tenant/organization data; every <see cref="FinancialStatementGroup"/>
/// member is a fixed platform-level reporting bucket (see that enum's doc comment).</summary>
public static class FinancialStatementGroupDisplay
{
    public static string ToDisplayName(this FinancialStatementGroup group) => group switch
    {
        FinancialStatementGroup.CashAndBank => "Cash & Bank",
        FinancialStatementGroup.InvestmentsAndFixedDeposits => "Investments & Fixed Deposits",
        FinancialStatementGroup.Receivables => "Receivables",
        FinancialStatementGroup.AccruedInterestReceivable => "Accrued Interest Receivable",
        FinancialStatementGroup.OtherAssets => "Other Assets",
        FinancialStatementGroup.AccountsPayable => "Accounts Payable",
        FinancialStatementGroup.ResidentAdvances => "Resident Advances",
        FinancialStatementGroup.OtherLiabilities => "Other Liabilities",
        FinancialStatementGroup.FundBalance => "Fund Balance",
        FinancialStatementGroup.AccumulatedSurplus => "Accumulated Surplus",
        FinancialStatementGroup.ServiceChargeIncome => "Service Charge Income",
        FinancialStatementGroup.UtilityGasIncome => "Utility / Gas Income",
        FinancialStatementGroup.FineIncome => "Fine Income",
        FinancialStatementGroup.InterestIncome => "Interest Income",
        FinancialStatementGroup.OtherIncome => "Other Income",
        FinancialStatementGroup.OperatingExpenses => "Operating Expenses",
        FinancialStatementGroup.InterestExpense => "Interest Expense",
        FinancialStatementGroup.OtherExpenses => "Other Expenses",
        _ => group.ToString(),
    };
}
