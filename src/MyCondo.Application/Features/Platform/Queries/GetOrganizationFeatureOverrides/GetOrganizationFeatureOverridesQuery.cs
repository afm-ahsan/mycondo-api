using Mediator;
using MyCondo.Application.Features.Platform.DTOs;

namespace MyCondo.Application.Features.Platform.Queries.GetOrganizationFeatureOverrides;

public sealed record GetOrganizationFeatureOverridesQuery(Guid OrganizationId) : IRequest<List<TenantFeatureOverrideDto>>;
