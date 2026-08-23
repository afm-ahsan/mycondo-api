namespace MyCondo.Application.Common.Abstractions;

/// <summary>File format a report can be exported to. Every renderer this enum names must exist —
/// there is deliberately no "Excel"/"Word" entry until an implementation backs it.</summary>
public enum ReportExportFormat
{
    Csv,
    Pdf,
}

/// <summary>One data column in an exported report table.</summary>
public sealed record ReportExportColumn(string Header, bool IsNumeric = false);

/// <summary>Format-agnostic description of a report ready to render. Every report exported through
/// <see cref="IReportExportService"/> — present and future — is expressed as one of these; only a
/// small per-report mapper (DTO → this record) is report-specific, never the rendering itself.</summary>
public sealed record ReportExportDocument(
    string Title,
    IReadOnlyList<(string Label, string Value)> MetadataLines,
    IReadOnlyList<ReportExportColumn> Columns,
    IReadOnlyList<IReadOnlyList<string>> Rows,
    IReadOnlyList<(string Label, string Value)>? Totals = null);

/// <summary>A generated export ready to stream back to the caller.</summary>
public sealed record ReportExportResult(Stream Content, string ContentType, string FileName);

/// <summary>Centralized, format-agnostic report export. Renders <see cref="ReportExportDocument"/>
/// to CSV or PDF (headless Chromium via Playwright) — the same two renderers serve every report in
/// the system. See mycondo-docs 05-MVP1-Report-Exports-Revised for the architecture decision.</summary>
public interface IReportExportService
{
    Task<ReportExportResult> ExportAsync(
        ReportExportDocument document,
        ReportExportFormat format,
        string fileNameWithoutExtension,
        CancellationToken cancellationToken);
}
