using System.Text;
using AwesomeAssertions;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Domain.Abstractions;
using MyCondo.Infrastructure.Reports;
using NSubstitute;

namespace MyCondo.Infrastructure.IntegrationTests.Reports;

public class CsvReportRendererTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 15, 3, 0, 0, TimeSpan.Zero);

    private readonly IClock _clock = Substitute.For<IClock>();
    private CsvReportRenderer CreateRenderer()
    {
        _clock.UtcNow.Returns(Now);
        return new CsvReportRenderer(_clock);
    }

    [Fact]
    public void Render_Starts_With_Utf8_Bom()
    {
        byte[] bytes = CreateRenderer().Render(SampleDocument());

        bytes.Take(3).Should().Equal(Encoding.UTF8.GetPreamble());
    }

    [Fact]
    public void Render_Includes_Title_Metadata_Header_Rows_And_Totals()
    {
        byte[] bytes = CreateRenderer().Render(SampleDocument());
        string csv = Encoding.UTF8.GetString(bytes.Skip(3).ToArray());

        csv.Should().Contain("Trial Balance");
        csv.Should().Contain("As Of,2026-08-15");
        csv.Should().Contain("Code,Account,Debit");
        csv.Should().Contain("1000,Cash,\"1,000.00\"");
        csv.Should().Contain("Total Debit,\"1,000.00\"");
        // Now is 03:00 UTC; Asia/Dhaka is UTC+6 with no DST, so local display time is 09:00.
        csv.Should().Contain("Generated At,2026-08-15 09:00 Asia/Dhaka");
    }

    [Fact]
    public void Render_Escapes_Cells_Containing_Commas_Quotes_And_Newlines()
    {
        ReportExportDocument document = new(
            "Sample",
            [],
            [new ReportExportColumn("Name")],
            [["Smith, \"Big\" & Co.\nLine2"]]);

        byte[] bytes = CreateRenderer().Render(document);
        string csv = Encoding.UTF8.GetString(bytes.Skip(3).ToArray());

        csv.Should().Contain("\"Smith, \"\"Big\"\" & Co.\nLine2\"");
    }

    private static ReportExportDocument SampleDocument() => new(
        "Trial Balance",
        [("As Of", "2026-08-15"), ("Scope", "Tenant (all accounts)")],
        [new ReportExportColumn("Code"), new ReportExportColumn("Account"), new ReportExportColumn("Debit", IsNumeric: true)],
        [["1000", "Cash", "1,000.00"]],
        [("Total Debit", "1,000.00")]);
}
