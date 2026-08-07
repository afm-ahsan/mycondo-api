using MyCondo.Domain.Features.Leasing.OccupancyRegistrations;

namespace MyCondo.Domain.Features.Leasing.OccupancyRegistrationStatusHistories;

public interface IOccupancyRegistrationStatusHistoryRepository
{
    Task<IReadOnlyList<OccupancyRegistrationStatusHistory>> GetForRegistrationAsync(
        OccupancyRegistrationId occupancyRegistrationId, CancellationToken cancellationToken);

    void Add(OccupancyRegistrationStatusHistory entry);
}
