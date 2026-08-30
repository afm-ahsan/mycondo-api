using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Domain.Features.Utilities.Common;
using MyCondo.Domain.Features.Utilities.Meters;

namespace MyCondo.Application.Features.Utilities.Common;

/// <summary>Resolves the commercial FeatureKey for any request carrying a <c>MeterId</c> (ADR-033 Task
/// 09A §15) — one dedicated implementation shared by the whole "MeterId" request family via the generic
/// constraint.</summary>
public sealed class MeterFeatureResolver<TRequest>(
    IMeterRepository meters
) : IRequestFeatureResolver<TRequest>
    where TRequest : IHasMeterId
{
    public async Task<string> ResolveFeatureKeyAsync(TRequest request, Guid tenantId, CancellationToken cancellationToken)
    {
        MeterId id = new(request.MeterId);
        UtilityType utilityType = await meters.GetUtilityTypeAsync(tenantId, id, cancellationToken)
            ?? throw new NotFoundException(nameof(Meter), request.MeterId);

        return UtilityFeatureKeyMapper.ToFeatureKey(utilityType);
    }
}
