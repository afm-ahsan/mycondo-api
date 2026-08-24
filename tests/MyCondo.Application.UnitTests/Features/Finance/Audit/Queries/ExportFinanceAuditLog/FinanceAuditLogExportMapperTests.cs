using AwesomeAssertions;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Finance.Audit.DTOs;
using MyCondo.Application.Features.Finance.Audit.Queries.ExportFinanceAuditLog;

namespace MyCondo.Application.UnitTests.Features.Finance.Audit.Queries.ExportFinanceAuditLog;

public class FinanceAuditLogExportMapperTests
{
    [Fact]
    public void Maps_Metadata_Columns_And_Rows()
    {
        Guid actorId = Guid.NewGuid();
        DateTimeOffset occurredAt = new(2026, 8, 15, 10, 30, 0, TimeSpan.Zero);
        List<FinanceAuditLogEntryDto> entries =
        [
            new(Guid.NewGuid(), occurredAt, actorId, "Jane Doe", "Invoice.Void", "Invoice", "abc123", "Voided due to duplicate", "corr-1"),
        ];

        ReportExportDocument document = FinanceAuditLogExportMapper.ToExportDocument(entries);

        document.Title.Should().Be("Finance Audit Log");
        document.MetadataLines.Should().Contain(("Entries", "1"));
        document.Columns.Select(c => c.Header).Should().Equal("Occurred", "Actor", "Action", "Target", "Details");
        document.Rows.Should().ContainSingle();
        document.Rows[0].Should().Equal(
            "2026-08-15 10:30:00 UTC", "Jane Doe", "Invoice.Void", "Invoice #abc123", "Voided due to duplicate");
    }

    [Fact]
    public void Renders_Missing_Target_And_Details_As_Em_Dash()
    {
        List<FinanceAuditLogEntryDto> entries =
        [
            new(Guid.NewGuid(), DateTimeOffset.UtcNow, null, "System", "AccountingPeriod.Close", null, null, null, null),
        ];

        ReportExportDocument document = FinanceAuditLogExportMapper.ToExportDocument(entries);

        document.Rows[0][3].Should().Be("—");
        document.Rows[0][4].Should().Be("—");
    }
}
