using MyCondo.Domain.Features.Property.Flats;

namespace MyCondo.Application.Common.Services;

/// <summary>Resolves a flat's business-facing display name (building code + flat number, e.g. "AISHA
/// A8") so callers never need to surface a raw <see cref="FlatId"/> in a UI-facing DTO.</summary>
public interface IFlatDisplayNameResolver
{
    Task<string> ResolveAsync(FlatId flatId, CancellationToken cancellationToken);

    /// <summary>Batched form for list/paged results — resolves every id with two queries total instead
    /// of one round-trip per row.</summary>
    Task<IReadOnlyDictionary<FlatId, string>> ResolveManyAsync(
        IReadOnlyCollection<FlatId> flatIds, CancellationToken cancellationToken);
}
