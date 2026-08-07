using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Amenities.DTOs;
using MyCondo.Application.Features.Amenities.Mappings;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Amenities.BlackoutDates;
using MyCondo.Domain.Features.Amenities.Facilities;
using MyCondo.Domain.Features.Amenities.PoolSessions;
using MyCondo.Domain.Features.Billing.Invoices;
using MyCondo.Domain.Features.Property.Flats;

namespace MyCondo.Application.Features.Amenities.Commands.CheckInPoolSession;

/// <summary>
/// Every eligibility rule (capacity, child-unaccompanied, overdue balance, missing safety
/// acknowledgement, outside operating hours) is bypassable only by a caller holding
/// <c>pool.override</c> who also supplies <see cref="CheckInPoolSessionCommand.OverrideReason"/> —
/// same pattern as <c>CheckInGuestCommandHandler</c>'s blocked-guest override. Child access is
/// enforced structurally (an open Adult <see cref="PoolSession"/> for the same flat), not via an
/// exact-age comparison against <see cref="Facility.MinimumAgeUnaccompanied"/> — the command doesn't
/// collect an exact age, only an <see cref="PoolAgeCategory"/>; documented as a known simplification.
/// Operating-hours comparison uses <see cref="DhakaTimeZone"/> (same convention as domestic-worker/
/// service-provider time-window checks) since <see cref="Facility.OperatingHoursStart"/>/
/// <see cref="Facility.OperatingHoursEnd"/> are local wall-clock times, not UTC. A full-day closure
/// (<see cref="Domain.Features.Amenities.BlackoutDates.BlackoutDate"/>) remains a hard block, not
/// override-bypassable — only the daily operating window is treated as an eligibility rule.
/// </summary>
public sealed class CheckInPoolSessionCommandHandler(
    IFacilityRepository facilities,
    IBlackoutDateRepository blackoutDates,
    IPoolSessionRepository poolSessions,
    IFlatRepository flats,
    IInvoiceRepository invoices,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    IClock clock,
    ILogger<CheckInPoolSessionCommandHandler> logger
) : IRequestHandler<CheckInPoolSessionCommand, PoolSessionDto>
{
    public async ValueTask<PoolSessionDto> Handle(CheckInPoolSessionCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        FacilityId facilityId = new(command.FacilityId);
        Facility facility = await facilities.GetByIdAsync(facilityId, cancellationToken)
            ?? throw new NotFoundException(nameof(Facility), command.FacilityId);
        if (facility.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(Facility), command.FacilityId);
        }

        if (facility.FacilityType != FacilityType.SwimmingPool)
        {
            throw new ConflictException($"Facility {command.FacilityId} is not a swimming pool.");
        }

        if (!facility.IsActive)
        {
            throw new ConflictException($"Facility {command.FacilityId} is inactive and cannot be accessed.");
        }

        FlatId flatId = new(command.FlatId);
        Flat flat = await flats.GetByIdAsync(flatId, cancellationToken)
            ?? throw new NotFoundException(nameof(Flat), command.FlatId);
        if (flat.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(Flat), command.FlatId);
        }

        DateTimeOffset nowUtc = clock.UtcNow;
        DateOnly today = DateOnly.FromDateTime(nowUtc.UtcDateTime);

        IReadOnlyList<BlackoutDate> activeBlackouts = await blackoutDates.GetActiveForFacilityAsync(tenantId, facilityId, cancellationToken);
        if (activeBlackouts.Any(b => b.Covers(today)))
        {
            throw new ConflictException($"Facility {command.FacilityId} is closed today.");
        }

        PoolPersonType personType = Enum.Parse<PoolPersonType>(command.PersonType);
        PoolAgeCategory ageCategory = Enum.Parse<PoolAgeCategory>(command.AgeCategory);

        List<string> unmetRules = [];

        // Locks the facility row for the rest of this handler so two concurrent check-ins for the
        // same facility can't both pass the capacity count before either inserts — see
        // IFacilityRepository.LockForCapacityCheckAsync's doc comment for why this exists (unlike
        // booking overlap, a count threshold has no DB constraint to fall back on).
        await using IUnitOfWorkTransaction transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        await facilities.LockForCapacityCheckAsync(facilityId, cancellationToken);

        int openCount = await poolSessions.CountOpenAsync(tenantId, facilityId, cancellationToken);
        if (openCount >= facility.Capacity)
        {
            unmetRules.Add($"capacity ({facility.Capacity}) reached");
        }

        PoolSessionId? accompaniedBySessionId = null;
        if (ageCategory == PoolAgeCategory.Child)
        {
            PoolSession? adultSession = command.AccompaniedBySessionId is Guid rawSessionId
                ? await poolSessions.GetByIdAsync(new PoolSessionId(rawSessionId), cancellationToken)
                : null;

            bool validAccompaniment = adultSession is not null && adultSession.TenantId == tenantId
                && adultSession.FacilityId == facilityId && adultSession.FlatId == flatId
                && adultSession.AgeCategory == PoolAgeCategory.Adult && adultSession.ExitAtUtc is null;

            if (validAccompaniment)
            {
                accompaniedBySessionId = adultSession!.Id;
            }
            else
            {
                unmetRules.Add("child not accompanied by a currently checked-in adult from the same flat");
            }
        }

        if (facility.BlocksEntryIfAccountOverdue)
        {
            decimal outstandingBalance = await invoices.GetOutstandingBalanceForFlatAsync(tenantId, flatId, cancellationToken);
            if (outstandingBalance > 0)
            {
                unmetRules.Add($"flat has an outstanding balance of {outstandingBalance}");
            }
        }

        if (facility.RequiresSafetyAcknowledgement && !command.SafetyAcknowledged)
        {
            unmetRules.Add("safety acknowledgement not given");
        }

        if (facility.OperatingHoursStart is TimeOnly opStart && facility.OperatingHoursEnd is TimeOnly opEnd)
        {
            TimeOnly nowLocalTime = TimeOnly.FromDateTime(DhakaTimeZone.ToLocal(nowUtc).DateTime);
            bool withinHours = opStart <= opEnd
                ? nowLocalTime >= opStart && nowLocalTime <= opEnd
                : nowLocalTime >= opStart || nowLocalTime <= opEnd; // overnight window (e.g. 22:00–06:00)

            if (!withinHours)
            {
                unmetRules.Add($"outside operating hours ({opStart:HH\\:mm}–{opEnd:HH\\:mm} local)");
            }
        }

        if (unmetRules.Count > 0)
        {
            if (string.IsNullOrWhiteSpace(command.OverrideReason))
            {
                throw new ForbiddenException(
                    $"Cannot check in: {string.Join("; ", unmetRules)}. An override reason is required to proceed.");
            }

            if (!currentUser.HasPermission("pool.override"))
            {
                throw new ForbiddenException("Overriding pool capacity/eligibility rules requires the pool.override permission.");
            }
        }

        decimal? guestFeeAmount = personType == PoolPersonType.Guest ? facility.GuestFeeAmount : null;
        DateTimeOffset? safetyAcknowledgedAtUtc = command.SafetyAcknowledged ? nowUtc : null;

        PoolSession session = PoolSession.CheckIn(
            tenantId, facilityId, flatId, personType, ageCategory, accompaniedBySessionId, guestFeeAmount,
            safetyAcknowledgedAtUtc, currentUser.UserId, command.OverrideReason, nowUtc);

        poolSessions.Add(session);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        logger.LogInformation(
            "Pool session {PoolSessionId} checked in at facility {FacilityId}, flat {FlatId}, tenant {TenantId}",
            session.Id, facilityId, flatId, tenantId);

        return session.ToDto();
    }
}
