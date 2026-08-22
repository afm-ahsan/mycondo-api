using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Property.Flats;

namespace MyCondo.Domain.Features.Leasing.OccupancyRegistrations;

public interface IOccupancyRegistrationRepository
{
    Task<OccupancyRegistration?> GetByIdAsync(OccupancyRegistrationId id, CancellationToken cancellationToken);

    /// <summary><paramref name="search"/> matches the registration's primary occupant name/phone/email
    /// or the flat number, mirroring IFlatOwnershipRepository.SearchAsync's search semantics.</summary>
    Task<PagedResult<OccupancyRegistration>> SearchAsync(
        Guid tenantId, FlatId? flatId, OccupancyRegistrationStatus? status, string? search, int page, int pageSize,
        CancellationToken cancellationToken);

    /// <summary>The single Active registration for a flat, if any — used to block a second concurrent
    /// active occupancy and to resolve "who currently lives here" for security/access checks.</summary>
    Task<OccupancyRegistration?> GetActiveForFlatAsync(Guid tenantId, FlatId flatId, CancellationToken cancellationToken);

    void Add(OccupancyRegistration registration);
}
