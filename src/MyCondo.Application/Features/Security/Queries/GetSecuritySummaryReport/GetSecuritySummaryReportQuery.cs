using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Security.DTOs;

namespace MyCondo.Application.Features.Security.Queries.GetSecuritySummaryReport;

public sealed record GetSecuritySummaryReportQuery : IRequest<SecuritySummaryDto>, ILifecycleReadOperation;
