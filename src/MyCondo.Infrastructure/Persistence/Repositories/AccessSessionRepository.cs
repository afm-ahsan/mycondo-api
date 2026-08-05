using Microsoft.EntityFrameworkCore;
using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Security.AccessSessions;
using MyCondo.Domain.Features.Security.DomesticWorkers;
using MyCondo.Domain.Features.Security.Guests;
using MyCondo.Domain.Features.Security.ServiceProviders;
using MyCondo.Domain.Features.Security.Vehicles;

namespace MyCondo.Infrastructure.Persistence.Repositories;

public sealed class AccessSessionRepository(MyCondoDbContext db) : IAccessSessionRepository
{
    public Task<AccessSession?> GetByIdAsync(AccessSessionId id, CancellationToken cancellationToken) =>
        db.Set<AccessSession>().FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public Task<AccessSession?> GetOpenSessionForGuestAsync(
        Guid tenantId, GuestProfileId guestProfileId, CancellationToken cancellationToken) =>
        db.Set<AccessSession>().FirstOrDefaultAsync(
            s => s.TenantId == tenantId
                 && s.GuestProfileId == guestProfileId
                 && s.Status == AccessSessionStatus.CheckedIn,
            cancellationToken);

    public Task<AccessSession?> GetOpenSessionForVehicleAsync(
        Guid tenantId, VehicleId vehicleId, CancellationToken cancellationToken) =>
        db.Set<AccessSession>().FirstOrDefaultAsync(
            s => s.TenantId == tenantId
                 && s.VehicleId == vehicleId
                 && s.Status == AccessSessionStatus.CheckedIn,
            cancellationToken);

    public Task<AccessSession?> GetOpenSessionForDomesticWorkerAsync(
        Guid tenantId, DomesticWorkerProfileId domesticWorkerProfileId, CancellationToken cancellationToken) =>
        db.Set<AccessSession>().FirstOrDefaultAsync(
            s => s.TenantId == tenantId
                 && s.DomesticWorkerProfileId == domesticWorkerProfileId
                 && s.Status == AccessSessionStatus.CheckedIn,
            cancellationToken);

    public Task<AccessSession?> GetOpenSessionForServiceProviderAsync(
        Guid tenantId, ServiceProviderProfileId serviceProviderProfileId, CancellationToken cancellationToken) =>
        db.Set<AccessSession>().FirstOrDefaultAsync(
            s => s.TenantId == tenantId
                 && s.ServiceProviderProfileId == serviceProviderProfileId
                 && s.Status == AccessSessionStatus.CheckedIn,
            cancellationToken);

    public async Task<PagedResult<AccessSession>> SearchCurrentlyInsideAsync(
        Guid tenantId,
        AccessCategory? category,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        IQueryable<AccessSession> query = db.Set<AccessSession>()
            .AsNoTracking()
            .Where(s => s.TenantId == tenantId && s.Status == AccessSessionStatus.CheckedIn);

        if (category is not null)
        {
            query = query.Where(s => s.AccessCategory == category);
        }

        long total = await query.LongCountAsync(cancellationToken);

        List<AccessSession> items = await query
            .OrderByDescending(s => s.EntryAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<AccessSession>(items, page, pageSize, total);
    }

    public async Task<PagedResult<AccessSession>> SearchForGuestProfileAsync(
        Guid tenantId,
        GuestProfileId guestProfileId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        IQueryable<AccessSession> query = db.Set<AccessSession>()
            .AsNoTracking()
            .Where(s => s.TenantId == tenantId && s.GuestProfileId == guestProfileId);

        long total = await query.LongCountAsync(cancellationToken);

        List<AccessSession> items = await query
            .OrderByDescending(s => s.EntryAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<AccessSession>(items, page, pageSize, total);
    }

    public async Task<PagedResult<AccessSession>> SearchForVehicleAsync(
        Guid tenantId,
        VehicleId vehicleId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        IQueryable<AccessSession> query = db.Set<AccessSession>()
            .AsNoTracking()
            .Where(s => s.TenantId == tenantId && s.VehicleId == vehicleId);

        long total = await query.LongCountAsync(cancellationToken);

        List<AccessSession> items = await query
            .OrderByDescending(s => s.EntryAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<AccessSession>(items, page, pageSize, total);
    }

    public async Task<PagedResult<AccessSession>> SearchForDomesticWorkerAsync(
        Guid tenantId,
        DomesticWorkerProfileId domesticWorkerProfileId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        IQueryable<AccessSession> query = db.Set<AccessSession>()
            .AsNoTracking()
            .Where(s => s.TenantId == tenantId && s.DomesticWorkerProfileId == domesticWorkerProfileId);

        long total = await query.LongCountAsync(cancellationToken);

        List<AccessSession> items = await query
            .OrderByDescending(s => s.EntryAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<AccessSession>(items, page, pageSize, total);
    }

    public async Task<PagedResult<AccessSession>> SearchForServiceProviderAsync(
        Guid tenantId,
        ServiceProviderProfileId serviceProviderProfileId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        IQueryable<AccessSession> query = db.Set<AccessSession>()
            .AsNoTracking()
            .Where(s => s.TenantId == tenantId && s.ServiceProviderProfileId == serviceProviderProfileId);

        long total = await query.LongCountAsync(cancellationToken);

        List<AccessSession> items = await query
            .OrderByDescending(s => s.EntryAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<AccessSession>(items, page, pageSize, total);
    }

    public void Add(AccessSession accessSession) => db.Set<AccessSession>().Add(accessSession);
}
