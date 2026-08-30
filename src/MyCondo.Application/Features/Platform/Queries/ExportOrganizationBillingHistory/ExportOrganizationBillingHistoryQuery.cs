using Mediator;
using MyCondo.Application.Common.Abstractions;

namespace MyCondo.Application.Features.Platform.Queries.ExportOrganizationBillingHistory;

public sealed record ExportOrganizationBillingHistoryQuery(
    Guid OrganizationId,
    DateOnly? DateFrom,
    DateOnly? DateTo,
    ReportExportFormat Format
) : IRequest<ReportExportResult>;
