using Mediator;
using MyCondo.Application.Features.Platform.DTOs;

namespace MyCondo.Application.Features.Platform.Queries.GetOrganizationSummaryStats;

public sealed record GetOrganizationSummaryStatsQuery : IRequest<OrganizationSummaryStatsDto>;
