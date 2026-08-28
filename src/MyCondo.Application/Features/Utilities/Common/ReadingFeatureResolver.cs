using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Domain.Features.Utilities.Common;
using MyCondo.Domain.Features.Utilities.Readings;

namespace MyCondo.Application.Features.Utilities.Common;

/// <summary>Resolves the commercial FeatureKey for any request carrying a <c>ReadingId</c> (ADR-033 Task
/// 09A §16) — <see cref="Reading"/> carries its own denormalized <see cref="UtilityType"/>, so this reads
/// it directly rather than joining through Meter. One dedicated implementation shared by the whole
/// "ReadingId" request family via the generic constraint.</summary>
public sealed class ReadingFeatureResolver<TRequest>(
    IReadingRepository readings
) : IRequestFeatureResolver<TRequest>
    where TRequest : IHasReadingId
{
    public async Task<string> ResolveFeatureKeyAsync(TRequest request, Guid tenantId, CancellationToken cancellationToken)
    {
        ReadingId id = new(request.ReadingId);
        UtilityType utilityType = await readings.GetUtilityTypeAsync(tenantId, id, cancellationToken)
            ?? throw new NotFoundException(nameof(Reading), request.ReadingId);

        return UtilityFeatureKeyMapper.ToFeatureKey(utilityType);
    }
}
