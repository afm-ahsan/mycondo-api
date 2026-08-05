using MyCondo.Domain.Features.Security.Parcels;

namespace MyCondo.Domain.Features.Security.ParcelCustodyEvents;

public interface IParcelCustodyEventRepository
{
    Task<List<ParcelCustodyEvent>> GetForParcelAsync(Guid tenantId, ParcelId parcelId, CancellationToken cancellationToken);

    void Add(ParcelCustodyEvent custodyEvent);
}
