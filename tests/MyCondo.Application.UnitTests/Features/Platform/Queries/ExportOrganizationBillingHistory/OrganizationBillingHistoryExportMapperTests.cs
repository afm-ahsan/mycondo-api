using AwesomeAssertions;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Platform.DTOs;
using MyCondo.Application.Features.Platform.Queries.ExportOrganizationBillingHistory;

namespace MyCondo.Application.UnitTests.Features.Platform.Queries.ExportOrganizationBillingHistory;

public class OrganizationBillingHistoryExportMapperTests
{
    [Fact]
    public void Maps_Invoice_And_Payment_Events_To_Distinct_Labeled_Rows()
    {
        Guid invoiceId = Guid.NewGuid();
        List<OrganizationBillingHistoryEventDto> events =
        [
            new(new DateOnly(2026, 8, 20), "PaymentRecorded", invoiceId, "SUBINV-1", "BDT", 5000m, null, null, null, Guid.NewGuid(), "REF-1"),
            new(new DateOnly(2026, 8, 1), "InvoiceIssued", invoiceId, "SUBINV-1", "BDT", 5000m, "Paid", 0m, null, null, null),
        ];

        ReportExportDocument document = OrganizationBillingHistoryExportMapper.ToExportDocument(
            events, new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 31));

        document.Title.Should().Be("Organization Billing History");
        document.Rows.Should().Contain(r => r[1] == "Payment Recorded" && r[8] == "REF-1");
        document.Rows.Should().Contain(r => r[1] == "Invoice Issued" && r[3] == "Paid" && r[6] == "0.00");
    }
}
