using Mediator;
using MyCondo.Application.Features.Security.DTOs;

namespace MyCondo.Application.Features.Security.Queries.GetSecuritySummaryReport;

public sealed record GetSecuritySummaryReportQuery : IRequest<SecuritySummaryDto>;
