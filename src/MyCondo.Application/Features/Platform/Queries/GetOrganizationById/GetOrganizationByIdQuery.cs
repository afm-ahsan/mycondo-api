using Mediator;
using MyCondo.Application.Features.Tenancy.Queries.GetTenantBySlug;

namespace MyCondo.Application.Features.Platform.Queries.GetOrganizationById;

public sealed record GetOrganizationByIdQuery(Guid OrganizationId) : IRequest<TenantSummaryDto>;
