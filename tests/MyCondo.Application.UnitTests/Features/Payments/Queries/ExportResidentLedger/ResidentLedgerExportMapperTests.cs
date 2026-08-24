using AwesomeAssertions;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Payments.DTOs;
using MyCondo.Application.Features.Payments.Queries.ExportResidentLedger;

namespace MyCondo.Application.UnitTests.Features.Payments.Queries.ExportResidentLedger;

public class ResidentLedgerExportMapperTests
{
    [Fact]
    public void Maps_Metadata_Columns_Rows_And_Totals()
    {
        LedgerEntryDto entry = new(
            Guid.NewGuid(), Guid.NewGuid(), "Receivable", null, "Debit", 500m,
            new DateOnly(2026, 8, 5), "Invoice charge", DateTimeOffset.UtcNow, "Invoice", Guid.NewGuid());

        ReportExportDocument document = ResidentLedgerExportMapper.ToExportDocument(
            [entry], new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 31));

        document.Title.Should().Be("Resident Ledger");
        document.MetadataLines.Should().Contain(("From", "2026-08-01"));
        document.MetadataLines.Should().Contain(("To", "2026-08-31"));
        document.Columns.Select(c => c.Header).Should()
            .Equal("Date", "Description", "Reference Type", "Direction", "Amount");
        document.Rows.Should().ContainSingle();
        document.Rows[0].Should().Equal("2026-08-05", "Invoice charge", "Invoice", "Debit", "500.00");
        document.Totals.Should().Contain(("Total Debit", "500.00"));
        document.Totals.Should().Contain(("Total Credit", "0.00"));
    }

    [Fact]
    public void Missing_Date_Range_Maps_To_Empty_Metadata_Values()
    {
        ReportExportDocument document = ResidentLedgerExportMapper.ToExportDocument([], null, null);

        document.MetadataLines.Should().Contain(("From", string.Empty));
        document.MetadataLines.Should().Contain(("To", string.Empty));
        document.Rows.Should().BeEmpty();
    }
}
