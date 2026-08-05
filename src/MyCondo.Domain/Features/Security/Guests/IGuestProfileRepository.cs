using MyCondo.Domain.Common;

namespace MyCondo.Domain.Features.Security.Guests;

public interface IGuestProfileRepository
{
    Task<GuestProfile?> GetByIdAsync(GuestProfileId id, CancellationToken cancellationToken);

    Task<GuestProfile?> GetByPhoneAsync(Guid tenantId, string phone, CancellationToken cancellationToken);

    Task<PagedResult<GuestProfile>> SearchAsync(
        Guid tenantId,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    void Add(GuestProfile guestProfile);
}
