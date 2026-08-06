using MyCondo.Domain.Features.Property.Flats;

namespace MyCondo.Domain.Features.Payments.ResidentAccounts;

public interface IResidentAccountRepository
{
    Task<ResidentAccount?> GetByIdAsync(ResidentAccountId id, CancellationToken cancellationToken);

    Task<ResidentAccount?> GetByFlatIdAsync(Guid tenantId, FlatId flatId, CancellationToken cancellationToken);

    void Add(ResidentAccount account);
}
