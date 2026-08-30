using Mediator;
using MyCondo.Application.Common.Abstractions;

namespace MyCondo.Application.Features.Platform.Queries.ExportPlatformBillingSummary;

public sealed record ExportPlatformBillingSummaryQuery(
    Guid? OrganizationId,
    DateOnly? DateFrom,
    DateOnly? DateTo,
    ReportExportFormat Format
) : IRequest<ReportExportResult>;
