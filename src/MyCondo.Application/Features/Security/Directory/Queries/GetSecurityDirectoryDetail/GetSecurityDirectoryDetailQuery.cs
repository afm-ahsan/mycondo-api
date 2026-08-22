using Mediator;
using MyCondo.Application.Features.Security.Directory.DTOs;

namespace MyCondo.Application.Features.Security.Directory.Queries.GetSecurityDirectoryDetail;

/// <summary><paramref name="ResidentType"/> is "Owner" or "Tenant" — together with
/// <paramref name="EntryId"/> (the <c>FlatOwnershipId</c> or <c>OccupancyRegistrationId</c> respectively)
/// this resolves the underlying aggregate, since the two id spaces aren't namespaced against each
/// other.</summary>
public sealed record GetSecurityDirectoryDetailQuery(
    Guid EntryId, string ResidentType
) : IRequest<SecurityDirectoryDetailDto>;
