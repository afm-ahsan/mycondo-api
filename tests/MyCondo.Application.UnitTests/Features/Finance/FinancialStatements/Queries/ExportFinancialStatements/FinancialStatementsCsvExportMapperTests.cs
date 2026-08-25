using AwesomeAssertions;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Finance.FinancialStatements.Notes;
using MyCondo.Application.Features.Finance.FinancialStatements.Queries.ExportFinancialStatements;
using MyCondo.Application.Features.Finance.FinancialStatements.Queries.GetIncomeExpenditureStatement;
using MyCondo.Application.Features.Finance.FinancialStatements.Queries.GetStatementOfFinancialPosition;
using MyCondo.Application.Features.Finance.Reports.Contracts;
using MyCondo.Domain.Features.Finance.ChartOfAccounts;

namespace MyCondo.Application.UnitTests.Features.Finance.FinancialStatements.Queries.ExportFinancialStatements;

public class FinancialStatementsCsvExportMapperTests
{
    private static readonly DateOnly StartDate = new(2026, 6, 1);
    private static readonly DateOnly EndDate = new(2026, 6, 30);

    private static StatementOfFinancialPositionAccountLineDto Account(string code, string name, decimal amount) =>
        new(Guid.NewGuid(), code, name, amount);

    [Fact]
    public void Title_And_Metadata_Reflect_The_Period_AsOfDate_Fund_And_Currency()
    {
        StatementOfFinancialPositionDto position = SamplePosition();
        IncomeExpenditureStatementDto incomeExpenditure = SampleIncomeExpenditure();

        ReportExportDocument document = FinancialStatementsCsvExportMapper.ToExportDocument(
            position, incomeExpenditure, [], "Reserve Fund");

        document.Title.Should().Be("Financial Statements");
        document.MetadataLines.Should().Contain(("Reporting Period", "2026-06-01 to 2026-06-30"));
        document.MetadataLines.Should().Contain(("Financial Position As Of", "2026-06-30"));
        document.MetadataLines.Should().Contain(("Fund", "Reserve Fund"));
        document.MetadataLines.Should().Contain(("Currency", "BDT"));
    }

    [Fact]
    public void Columns_Match_The_Task_8_Spec_Exactly()
    {
        ReportExportDocument document = FinancialStatementsCsvExportMapper.ToExportDocument(
            SamplePosition(), SampleIncomeExpenditure(), [], "All Funds");

        document.Columns.Select(c => c.Header).Should().Equal(
            "Statement", "Section", "Group", "AccountOrLine", "Amount",
            "NoteKey", "NoteTitle", "ReconciliationStatus", "GlBalance", "ScheduleBalance", "Difference");
        document.Columns.Where(c => c.IsNumeric).Select(c => c.Header).Should().BeEquivalentTo(
            ["Amount", "GlBalance", "ScheduleBalance", "Difference"]);
    }

    [Fact]
    public void Financial_Position_Emits_Account_Group_Subtotal_And_Section_Total_Rows()
    {
        StatementOfFinancialPositionDto position = SamplePosition(
            assets:
            [
                new StatementOfFinancialPositionGroupDto(
                    FinancialStatementGroup.CashAndBank, 100_000m, [Account("1000", "Operating Account", 100_000m)]),
            ],
            totalAssets: 100_000m);

        ReportExportDocument document = FinancialStatementsCsvExportMapper.ToExportDocument(
            position, SampleIncomeExpenditure(), [], "All Funds");

        document.Rows.Should().ContainSingle(r => r[3] == "1000 - Operating Account" && r[4] == "100,000.00" && r[0] == "Financial Position");
        document.Rows.Should().ContainSingle(r => r[3] == "Cash & Bank (Subtotal)" && r[4] == "100,000.00");
        document.Rows.Should().ContainSingle(r => r[3] == "TOTAL ASSETS" && r[4] == "100,000.00");
    }

    [Fact]
    public void Funds_Section_Includes_A_Cumulative_Surplus_Deficit_Row()
    {
        StatementOfFinancialPositionDto position = SamplePosition(cumulativeSurplusDeficit: 25_000m);

        ReportExportDocument document = FinancialStatementsCsvExportMapper.ToExportDocument(
            position, SampleIncomeExpenditure(), [], "All Funds");

        document.Rows.Should().ContainSingle(r =>
            r[0] == "Financial Position" && r[1] == "Funds" && r[3] == "Cumulative Surplus / (Deficit)" && r[4] == "25,000.00");
    }

    [Fact]
    public void Income_Expenditure_Emits_A_Surplus_Deficit_Row()
    {
        IncomeExpenditureStatementDto incomeExpenditure = SampleIncomeExpenditure(surplusDeficit: -5_000m);

        ReportExportDocument document = FinancialStatementsCsvExportMapper.ToExportDocument(
            SamplePosition(), incomeExpenditure, [], "All Funds");

        document.Rows.Should().ContainSingle(r =>
            r[0] == "Income & Expenditure" && r[3] == "SURPLUS / (DEFICIT)" && r[4] == "-5,000.00");
    }

    [Fact]
    public void A_Note_Emits_A_Summary_Row_Then_Its_Detail_Rows_From_Whichever_RowList_Is_Populated()
    {
        FinancialStatementNote note = new(
            FinancialStatementNoteKey.CashAndBank, "Cash & Bank", FinancialStatementNoteScope.AsOfDate,
            FinancialStatementGroup.CashAndBank, null, EndDate, null,
            GlBalance: 150_000m, ScheduleBalance: 150_000m, Difference: 0m, IsReconciled: true,
            UnattributedAmount: 0m, TotalRowCount: 1, IsDetailTruncated: false, Warnings: [],
            CashAndBankRows:
            [
                new CashAndBankNoteRow(
                    null, Guid.NewGuid(), "1000", "Operating Account", null, "City Bank", null, "3210",
                    null, true, 150_000m, false),
            ]);

        ReportExportDocument document = FinancialStatementsCsvExportMapper.ToExportDocument(
            SamplePosition(), SampleIncomeExpenditure(), [note], "All Funds");

        document.Rows.Should().ContainSingle(r =>
            r[0] == "Notes to Accounts" && r[3] == "Cash & Bank" && r[4] == "150,000.00" &&
            r[5] == "CashAndBank" && r[6] == "Cash & Bank" && r[7] == "Reconciled" &&
            r[8] == "150,000.00" && r[9] == "150,000.00" && r[10] == "0.00");

        document.Rows.Should().ContainSingle(r =>
            r[0] == "Notes to Accounts" && r[3].Contains("Operating Account") && r[4] == "150,000.00" &&
            r[5] == "CashAndBank");
    }

    [Fact]
    public void An_Unattributed_Detail_Row_Is_Marked_As_Such()
    {
        FinancialStatementNote note = new(
            FinancialStatementNoteKey.Payables, "Payables", FinancialStatementNoteScope.AsOfDate,
            FinancialStatementGroup.AccountsPayable, null, EndDate, null,
            GlBalance: 5_000m, ScheduleBalance: 3_000m, Difference: 2_000m, IsReconciled: false,
            UnattributedAmount: 2_000m, TotalRowCount: 1, IsDetailTruncated: false,
            Warnings: ["2,000.00 could not be traced to a subledger record."],
            PayableRows: [new PayableNoteRow(null, null, null, null, null, null, null, null, null, null, 2_000m, true, "Untraceable")]);

        ReportExportDocument document = FinancialStatementsCsvExportMapper.ToExportDocument(
            SamplePosition(), SampleIncomeExpenditure(), [note], "All Funds");

        document.Rows.Should().ContainSingle(r => r[3] == "Unattributed Payable Activity (Unattributed)" && r[4] == "2,000.00");
    }

    [Fact]
    public void Totals_Block_Carries_The_Reported_Grand_Totals_And_Balance_Status()
    {
        StatementOfFinancialPositionDto position = SamplePosition(
            totalAssets: 500_000m, totalLiabilities: 100_000m, totalFunds: 400_000m,
            totalLiabilitiesAndFunds: 500_000m, difference: 0m, isBalanced: true);
        IncomeExpenditureStatementDto incomeExpenditure = SampleIncomeExpenditure(
            totalIncome: 200_000m, totalExpenditure: 150_000m, surplusDeficit: 50_000m);

        ReportExportDocument document = FinancialStatementsCsvExportMapper.ToExportDocument(
            position, incomeExpenditure, [], "All Funds");

        document.Totals.Should().Contain(("Total Assets", "500,000.00"));
        document.Totals.Should().Contain(("Total Liabilities", "100,000.00"));
        document.Totals.Should().Contain(("Total Funds", "400,000.00"));
        document.Totals.Should().Contain(("Total Liabilities & Funds", "500,000.00"));
        document.Totals.Should().Contain(("Financial Position Status", "Balanced"));
        document.Totals.Should().Contain(("Total Income", "200,000.00"));
        document.Totals.Should().Contain(("Total Expenditure", "150,000.00"));
        document.Totals.Should().Contain(("Surplus / (Deficit)", "50,000.00"));
    }

    [Fact]
    public void An_Unbalanced_Position_Is_Reflected_In_The_Totals_Status()
    {
        StatementOfFinancialPositionDto position = SamplePosition(difference: 500m, isBalanced: false);

        ReportExportDocument document = FinancialStatementsCsvExportMapper.ToExportDocument(
            position, SampleIncomeExpenditure(), [], "All Funds");

        document.Totals.Should().Contain(("Financial Position Status", "Financial Integrity Warning"));
        document.Totals.Should().Contain(("Financial Position Difference", "500.00"));
    }

    private static StatementOfFinancialPositionDto SamplePosition(
        IReadOnlyList<StatementOfFinancialPositionGroupDto>? assets = null,
        decimal cumulativeSurplusDeficit = 0m,
        decimal totalAssets = 0m,
        decimal totalLiabilities = 0m,
        decimal totalFunds = 0m,
        decimal totalLiabilitiesAndFunds = 0m,
        decimal difference = 0m,
        bool isBalanced = true) => new(
        FinanceReportMetadataDto.ForAsOf(EndDate, "Tenant (all funds)", DateTimeOffset.UtcNow, null),
        null,
        new StatementOfFinancialPositionSectionDto(assets ?? [], totalAssets),
        new StatementOfFinancialPositionSectionDto([], totalLiabilities),
        new StatementOfFinancialPositionSectionDto([], totalFunds - cumulativeSurplusDeficit),
        cumulativeSurplusDeficit,
        totalAssets,
        totalLiabilities,
        totalFunds,
        totalLiabilitiesAndFunds,
        difference,
        isBalanced,
        []);

    private static IncomeExpenditureStatementDto SampleIncomeExpenditure(
        decimal totalIncome = 0m, decimal totalExpenditure = 0m, decimal surplusDeficit = 0m) => new(
        FinanceReportMetadataDto.ForPeriod(StartDate, EndDate, "Tenant (all funds)", DateTimeOffset.UtcNow, null),
        null,
        new IncomeExpenditureStatementSectionDto([], totalIncome),
        new IncomeExpenditureStatementSectionDto([], totalExpenditure),
        totalIncome,
        totalExpenditure,
        surplusDeficit,
        []);
}
