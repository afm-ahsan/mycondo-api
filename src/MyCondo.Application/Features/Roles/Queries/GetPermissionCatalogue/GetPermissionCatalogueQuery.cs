using Mediator;

namespace MyCondo.Application.Features.Roles.Queries.GetPermissionCatalogue;

public sealed record GetPermissionCatalogueQuery : IRequest<List<PermissionDto>>;
