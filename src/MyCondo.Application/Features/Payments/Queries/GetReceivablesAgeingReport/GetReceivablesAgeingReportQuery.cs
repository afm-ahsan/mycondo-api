using Mediator;
using MyCondo.Application.Features.Payments.DTOs;

namespace MyCondo.Application.Features.Payments.Queries.GetReceivablesAgeingReport;

public sealed record GetReceivablesAgeingReportQuery(
    Guid? BuildingId,
    DateOnly? AsOfDate
) : IRequest<ReceivablesAgeingReportDto>;
