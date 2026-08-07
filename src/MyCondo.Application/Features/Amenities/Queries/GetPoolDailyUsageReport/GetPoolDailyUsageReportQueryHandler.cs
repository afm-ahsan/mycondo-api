using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Amenities.DTOs;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Amenities.Facilities;
using MyCondo.Domain.Features.Amenities.PoolSessions;

namespace MyCondo.Application.Features.Amenities.Queries.GetPoolDailyUsageReport;

/// <summary>Peak concurrent occupancy is computed with a sweep over entry/exit events for the day —
/// sessions still open at query time count their exit as "now" for the sweep, and cross-midnight exits
/// are clamped to the end of the report day (occupancy for a later day is that day's own report's
/// concern, not double-counted here).</summary>
public sealed class GetPoolDailyUsageReportQueryHandler(
    IPoolSessionRepository poolSessions,
    ICurrentUserProvider currentUser,
    IClock clock
) : IRequestHandler<GetPoolDailyUsageReportQuery, PoolDailyUsageReportDto>
{
    public async ValueTask<PoolDailyUsageReportDto> Handle(GetPoolDailyUsageReportQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        FacilityId facilityId = new(query.FacilityId);
        DateTimeOffset fromUtc = new(query.Date, TimeOnly.MinValue, TimeSpan.Zero);
        DateTimeOffset toUtc = new(query.Date.AddDays(1), TimeOnly.MinValue, TimeSpan.Zero);

        IReadOnlyList<PoolSession> sessions = await poolSessions.GetForDateAsync(tenantId, facilityId, fromUtc, toUtc, cancellationToken);

        int residentEntries = sessions.Count(s => s.PersonType == PoolPersonType.Resident);
        int guestEntries = sessions.Count(s => s.PersonType == PoolPersonType.Guest);
        int adultEntries = sessions.Count(s => s.AgeCategory == PoolAgeCategory.Adult);
        int childEntries = sessions.Count(s => s.AgeCategory == PoolAgeCategory.Child);

        DateTimeOffset nowUtc = clock.UtcNow;
        List<(DateTimeOffset At, int Delta)> events = [];
        foreach (PoolSession session in sessions)
        {
            events.Add((session.EntryAtUtc, 1));
            DateTimeOffset exitAt = session.ExitAtUtc ?? nowUtc;
            if (exitAt > toUtc)
            {
                exitAt = toUtc;
            }

            events.Add((exitAt, -1));
        }

        int running = 0;
        int peak = 0;
        foreach ((DateTimeOffset _, int delta) in events.OrderBy(e => e.At))
        {
            running += delta;
            peak = Math.Max(peak, running);
        }

        return new PoolDailyUsageReportDto(query.Date, sessions.Count, residentEntries, guestEntries, adultEntries, childEntries, peak);
    }
}
