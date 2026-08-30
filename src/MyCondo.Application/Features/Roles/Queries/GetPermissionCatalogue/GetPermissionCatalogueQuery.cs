using Mediator;
using MyCondo.Application.Common.Abstractions;

namespace MyCondo.Application.Features.Roles.Queries.GetPermissionCatalogue;

public sealed record GetPermissionCatalogueQuery : IRequest<List<PermissionDto>>, ILifecycleReadOperation;
