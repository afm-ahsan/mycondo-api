using AwesomeAssertions;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Finance.Integrity.DTOs;
using MyCondo.Application.Features.Finance.Integrity.Queries.ExportFinancialIntegrityDashboard;

namespace MyCondo.Application.UnitTests.Features.Finance.Integrity.Queries.ExportFinancialIntegrityDashboard;

public class FinancialIntegrityDashboardExportMapperTests
{
    [Fact]
    public void Maps_Healthy_Dashboard_To_Five_Check_Rows()
    {
        FinancialIntegrityDashboardDto dashboard = new(0, 0, 0, 0, 0, true);

        ReportExportDocument document = FinancialIntegrityDashboardExportMapper.ToExportDocument(dashboard);

        document.Title.Should().Be("Financial Integrity Dashboard");
        document.MetadataLines.Should().Contain(("Overall Status", "Healthy"));
        document.Columns.Select(c => c.Header).Should().Equal("Category", "Check", "Description", "Count", "Status");
        document.Rows.Should().HaveCount(5);
        document.Rows.Should().OnlyContain(row => row[4] == "Healthy");
    }

    [Fact]
    public void Marks_NonZero_Checks_As_Attention_And_Overall_Status_As_Attention_Required()
    {
        FinancialIntegrityDashboardDto dashboard = new(2, 0, 0, 3, 1, false);

        ReportExportDocument document = FinancialIntegrityDashboardExportMapper.ToExportDocument(dashboard);

        document.MetadataLines.Should().Contain(("Overall Status", "Attention required"));
        document.Rows[0].Should().Equal(
            "Data Integrity", "Unbalanced postings",
            "Ledger postings whose debits and credits do not sum to zero.", "2", "Attention");
        document.Rows[1][4].Should().Be("Healthy");
        document.Rows[3].Should().Equal(
            "Operational Backlog", "Stale unreconciled bank items",
            "Bank statement lines still unmatched more than 45 days after the statement date.", "3", "Attention");
    }
}
