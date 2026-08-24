using System.Globalization;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Finance.Audit.DTOs;

namespace MyCondo.Application.Features.Finance.Audit.Queries.ExportFinanceAuditLog;

public static class FinanceAuditLogExportMapper
{
    private const string NoValue = "—";

    public static ReportExportDocument ToExportDocument(IReadOnlyList<FinanceAuditLogEntryDto> entries)
    {
        List<(string Label, string Value)> metadataLines =
        [
            ("Entries", entries.Count.ToString(CultureInfo.InvariantCulture)),
        ];

        List<ReportExportColumn> columns =
        [
            new("Occurred"),
            new("Actor"),
            new("Action"),
            new("Target"),
            new("Details"),
        ];

        List<IReadOnlyList<string>> rows = entries
            .Select(entry => (IReadOnlyList<string>)
            [
                entry.OccurredAtUtc.ToString("yyyy-MM-dd HH:mm:ss 'UTC'", CultureInfo.InvariantCulture),
                entry.ActorDisplayName,
                entry.Action,
                FormatTarget(entry),
                entry.Metadata ?? NoValue,
            ])
            .ToList();

        return new ReportExportDocument("Finance Audit Log", metadataLines, columns, rows);
    }

    private static string FormatTarget(FinanceAuditLogEntryDto entry) =>
        entry.TargetType is null
            ? NoValue
            : entry.TargetId is null
                ? entry.TargetType
                : $"{entry.TargetType} #{entry.TargetId}";
}
