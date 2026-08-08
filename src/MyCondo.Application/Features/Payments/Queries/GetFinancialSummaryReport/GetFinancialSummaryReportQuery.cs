using Mediator;
using MyCondo.Application.Features.Payments.DTOs;

namespace MyCondo.Application.Features.Payments.Queries.GetFinancialSummaryReport;

public sealed record GetFinancialSummaryReportQuery(
    Guid? BuildingId,
    DateOnly FromDate,
    DateOnly ToDate
) : IRequest<FinancialSummaryDto>;
