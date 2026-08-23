using MyCondo.Application.Common.Abstractions;

namespace MyCondo.Infrastructure.Reports;

/// <summary>Single entry point every report export goes through. Dispatches to the CSV or PDF
/// renderer by format — callers never construct a renderer directly.</summary>
public sealed class ReportExportService(
    CsvReportRenderer csvRenderer,
    ReportHtmlTemplate htmlTemplate,
    PlaywrightPdfRenderer pdfRenderer) : IReportExportService
{
    public async Task<ReportExportResult> ExportAsync(
        ReportExportDocument document,
        ReportExportFormat format,
        string fileNameWithoutExtension,
        CancellationToken cancellationToken)
    {
        switch (format)
        {
            case ReportExportFormat.Csv:
            {
                byte[] bytes = csvRenderer.Render(document);
                return new ReportExportResult(
                    new MemoryStream(bytes), "text/csv", $"{fileNameWithoutExtension}.csv");
            }

            case ReportExportFormat.Pdf:
            {
                string html = htmlTemplate.Render(document);
                byte[] bytes = await pdfRenderer.RenderPdfAsync(html, cancellationToken);
                return new ReportExportResult(
                    new MemoryStream(bytes), "application/pdf", $"{fileNameWithoutExtension}.pdf");
            }

            default:
                throw new ArgumentOutOfRangeException(nameof(format), format, "Unsupported report export format.");
        }
    }
}
