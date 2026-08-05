namespace MyCondo.Domain.Features.Security.Parcels;

/// <summary>
/// <see cref="ResidentNotified"/> is declared for schema completeness with the register digitization
/// spec's exact status list, but the current command set folds notify-and-await into a single
/// transition (<c>Received</c> → <c>AwaitingCollection</c>) since that matches real front-desk
/// workflow — front desk staff notify and the parcel is immediately awaiting pickup in the same
/// action. This value stays reachable for a future finer-grained multi-attempt notification flow.
/// </summary>
public enum ParcelStatus
{
    Received = 0,
    ResidentNotified = 1,
    AwaitingCollection = 2,
    Collected = 3,
    Returned = 4,
    Rejected = 5,
    Damaged = 6,
    LostOrEscalated = 7,
}
