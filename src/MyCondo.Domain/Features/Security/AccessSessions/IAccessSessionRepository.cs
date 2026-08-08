using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Security.DomesticWorkers;
using MyCondo.Domain.Features.Security.Guests;
using MyCondo.Domain.Features.Security.ServiceProviders;
using MyCondo.Domain.Features.Security.Vehicles;

namespace MyCondo.Domain.Features.Security.AccessSessions;

public interface IAccessSessionRepository
{
    Task<AccessSession?> GetByIdAsync(AccessSessionId id, CancellationToken cancellationToken);

    Task<AccessSession?> GetOpenSessionForGuestAsync(
        Guid tenantId, GuestProfileId guestProfileId, CancellationToken cancellationToken);

    Task<AccessSession?> GetOpenSessionForVehicleAsync(
        Guid tenantId, VehicleId vehicleId, CancellationToken cancellationToken);

    Task<AccessSession?> GetOpenSessionForDomesticWorkerAsync(
        Guid tenantId, DomesticWorkerProfileId domesticWorkerProfileId, CancellationToken cancellationToken);

    Task<AccessSession?> GetOpenSessionForServiceProviderAsync(
        Guid tenantId, ServiceProviderProfileId serviceProviderProfileId, CancellationToken cancellationToken);

    Task<PagedResult<AccessSession>> SearchCurrentlyInsideAsync(
        Guid tenantId,
        AccessCategory? category,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    /// <summary>COUNT of currently-inside (open, CheckedIn) sessions grouped by category — tenant-wide
    /// only, since <see cref="AccessSession"/> has no BuildingId of its own (a single front gate serves
    /// the whole property, not one per building).</summary>
    Task<IReadOnlyList<CurrentlyInsideCategoryCount>> GetCurrentlyInsideCountsByCategoryAsync(
        Guid tenantId, CancellationToken cancellationToken);

    Task<PagedResult<AccessSession>> SearchForGuestProfileAsync(
        Guid tenantId,
        GuestProfileId guestProfileId,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<PagedResult<AccessSession>> SearchForVehicleAsync(
        Guid tenantId,
        VehicleId vehicleId,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<PagedResult<AccessSession>> SearchForDomesticWorkerAsync(
        Guid tenantId,
        DomesticWorkerProfileId domesticWorkerProfileId,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<PagedResult<AccessSession>> SearchForServiceProviderAsync(
        Guid tenantId,
        ServiceProviderProfileId serviceProviderProfileId,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    void Add(AccessSession accessSession);
}
