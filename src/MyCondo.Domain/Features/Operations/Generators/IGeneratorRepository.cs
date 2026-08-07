using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Property.Buildings;

namespace MyCondo.Domain.Features.Operations.Generators;

public interface IGeneratorRepository
{
    Task<Generator?> GetByIdAsync(GeneratorId id, CancellationToken cancellationToken);

    Task<PagedResult<Generator>> SearchAsync(
        Guid tenantId, BuildingId? buildingId, int page, int pageSize, CancellationToken cancellationToken);

    /// <summary>Locks the generator row (<c>SELECT ... FOR UPDATE</c>) for the duration of the caller's
    /// transaction — used by <c>StartGeneratorSessionCommandHandler</c> to serialize concurrent
    /// session starts against the same generator, so the "is there already an open session" check
    /// can't race the way a plain read would. Mirrors
    /// <c>IFacilityRepository.LockForCapacityCheckAsync</c> (Slice G), added there for the identical
    /// reason: a business-rule threshold with no expressible DB constraint. Must be called inside an
    /// open <see cref="MyCondo.Domain.Abstractions.IUnitOfWork.BeginTransactionAsync"/> transaction —
    /// the lock is held until commit/rollback.</summary>
    Task LockForSessionStartCheckAsync(GeneratorId id, CancellationToken cancellationToken);

    void Add(Generator generator);
}
