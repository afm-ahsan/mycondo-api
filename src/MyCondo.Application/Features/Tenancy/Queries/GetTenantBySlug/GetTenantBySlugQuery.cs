using Mediator;

namespace MyCondo.Application.Features.Tenancy.Queries.GetTenantBySlug;

public sealed record GetTenantBySlugQuery(string Slug) : IRequest<TenantSummaryDto>;
