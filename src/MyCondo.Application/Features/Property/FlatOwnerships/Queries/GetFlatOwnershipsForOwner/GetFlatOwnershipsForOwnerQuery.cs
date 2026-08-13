using Mediator;

namespace MyCondo.Application.Features.Property.FlatOwnerships.Queries.GetFlatOwnershipsForOwner;

/// <summary>Every flat a given owner (Resident) owns — the backing query for the Flat Owner detail
/// page's "Ownership Information" panel, supporting one owner across multiple flats.</summary>
public sealed record GetFlatOwnershipsForOwnerQuery(Guid ResidentId) : IRequest<List<OwnerFlatOwnershipDto>>;
