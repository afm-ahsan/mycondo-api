using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Property.Flats;

namespace MyCondo.Domain.Features.Security.Parcels;

public interface IParcelRepository
{
    Task<Parcel?> GetByIdAsync(ParcelId id, CancellationToken cancellationToken);

    Task<PagedResult<Parcel>> SearchAsync(
        Guid tenantId,
        ParcelStatus? status,
        FlatId? recipientFlatId,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    void Add(Parcel parcel);
}
