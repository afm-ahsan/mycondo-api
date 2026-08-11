using Mediator;

namespace MyCondo.Application.Features.Property.FlatOwnerships.Queries.GetFlatOwnershipsForUser;

/// <summary>Every flat a given User owns — the backing query for the Flat Owner detail page's
/// "Ownership Information" panel, supporting one owner across multiple flats.</summary>
public sealed record GetFlatOwnershipsForUserQuery(Guid UserId) : IRequest<List<UserFlatOwnershipDto>>;
