using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Residents.DTOs;
using MyCondo.Domain.Common;

namespace MyCondo.Application.Features.Residents.Queries.GetResidentsForTenant;

/// <summary>Core (non-feature-gated) query pilot for the ADR-032 Task 10 lifecycle read/write
/// classification — proves an ordinary core read keeps working under a Restricted/Expired subscription.</summary>
public sealed record GetResidentsForTenantQuery(
    string? Search,
    int Page,
    int PageSize
) : IRequest<PagedResult<ResidentDto>>, ILifecycleReadOperation;
