using Mediator;
using MyCondo.Application.Features.Security.Directory.DTOs;
using MyCondo.Domain.Common;

namespace MyCondo.Application.Features.Security.Directory.Queries.GetSecurityDirectory;

/// <summary>Always restricted to active occupancies/ownerships server-side — security only needs to see
/// who currently has access, not the full registration/ownership history. <paramref name="Search"/>
/// matches name, flat number, or mobile number. <paramref name="AccessStatus"/> is
/// "Authorized"/"Revoked" when supplied.</summary>
public sealed record GetSecurityDirectoryQuery(
    string? Search, Guid? BuildingId, Guid? FlatId, string? AccessStatus, int Page, int PageSize
) : IRequest<PagedResult<SecurityDirectoryEntryDto>>;
