using System.Globalization;
using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Utilities.DTOs;
using MyCondo.Application.Features.Utilities.Queries.GetReadings;
using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Domain.Features.Utilities.Meters;

namespace MyCondo.Application.Features.Utilities.Queries.ExportConsumptionHistoryReport;

/// <summary>Reuses <see cref="GetReadingsQuery"/> — the same paginated query backing the on-screen
/// Consumption History list — for the actual reading data (tenant scoping, meterId filter), rather
/// than duplicating that logic or introducing a date range the screen doesn't filter by. The on-screen
/// browse caps at 100 rows, but export must return the FULL matching result set, so this handler pages
/// through <see cref="GetReadingsQuery"/> internally at <see cref="MaxPageSize"/> until every matching
/// reading has been retrieved (same loop-to-completion pattern as
/// <c>ExportResidentLedgerQueryHandler</c>). Meter/Building are resolved separately only for the
/// export's human-readable header metadata — <see cref="ReadingDto"/> carries raw GUIDs only.</summary>
public sealed class ExportConsumptionHistoryReportQueryHandler(
    ISender sender,
    IMeterRepository meters,
    IBuildingRepository buildings,
    ICurrentUserProvider currentUser,
    IReportExportService exportService
) : IRequestHandler<ExportConsumptionHistoryReportQuery, ReportExportResult>
{
    private const int MaxPageSize = 100;

    public async ValueTask<ReportExportResult> Handle(ExportConsumptionHistoryReportQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        MeterId meterId = new(query.MeterId);
        Meter meter = await meters.GetByIdAsync(meterId, cancellationToken)
            ?? throw new NotFoundException(nameof(Meter), query.MeterId);
        if (meter.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(Meter), query.MeterId);
        }

        Building? building = await buildings.GetByIdAsync(meter.BuildingId, cancellationToken);

        List<ReadingDto> allReadings = [];
        PagedResult<ReadingDto> lastPage;
        int page = 1;
        do
        {
            lastPage = await sender.Send(
                new GetReadingsQuery(query.MeterId, FlatId: null, Status: null, page, MaxPageSize),
                cancellationToken);
            allReadings.AddRange(lastPage.Items);
            page++;
        }
        while (lastPage.Items.Count > 0 && allReadings.Count < lastPage.Total);

        ReportExportDocument document = ConsumptionHistoryExportMapper.ToExportDocument(allReadings, meter, building);

        string fileName = $"consumption-history-{query.MeterId.ToString("N", CultureInfo.InvariantCulture)[..8]}";

        return await exportService.ExportAsync(document, query.Format, fileName, cancellationToken);
    }
}
