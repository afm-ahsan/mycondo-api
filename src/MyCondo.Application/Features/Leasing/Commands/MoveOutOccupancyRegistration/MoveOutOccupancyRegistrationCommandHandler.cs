using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Leasing.DTOs;
using MyCondo.Application.Features.Leasing.Mappings;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Leasing.HouseholdMembers;
using MyCondo.Domain.Features.Leasing.OccupancyRegistrations;
using MyCondo.Domain.Features.Leasing.OccupancyRegistrationStatusHistories;

namespace MyCondo.Application.Features.Leasing.Commands.MoveOutOccupancyRegistration;

/// <summary>
/// Ends an active occupancy and, in the same transaction, deactivates every household member on the
/// registration — this is the cascade the Security/access-session modules rely on to stop treating
/// this flat's former occupants as currently-valid (mirrors <c>DomesticWorkerAssignment.Deactivate</c>'s
/// role in removing access eligibility once a worker is no longer engaged).
/// </summary>
public sealed class MoveOutOccupancyRegistrationCommandHandler(
    IOccupancyRegistrationRepository registrations,
    IHouseholdMemberRepository members,
    IOccupancyRegistrationStatusHistoryRepository history,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    IClock clock,
    ILogger<MoveOutOccupancyRegistrationCommandHandler> logger
) : IRequestHandler<MoveOutOccupancyRegistrationCommand, OccupancyRegistrationDto>
{
    public async ValueTask<OccupancyRegistrationDto> Handle(
        MoveOutOccupancyRegistrationCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        OccupancyRegistrationId id = new(command.OccupancyRegistrationId);
        OccupancyRegistration registration = await registrations.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(OccupancyRegistration), command.OccupancyRegistrationId);
        if (registration.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(OccupancyRegistration), command.OccupancyRegistrationId);
        }

        OccupancyRegistrationStatus fromStatus = registration.Status;
        registration.MoveOut(command.Reason, clock.UtcNow);

        IReadOnlyList<HouseholdMember> registrationMembers = await members.GetForRegistrationAsync(id, cancellationToken);
        foreach (HouseholdMember member in registrationMembers.Where(m => m.IsActive))
        {
            member.Deactivate();
        }

        history.Add(OccupancyRegistrationStatusHistory.Record(
            tenantId, id, fromStatus, registration.Status, currentUser.UserId, command.Reason, clock.UtcNow));

        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Occupancy registration {OccupancyRegistrationId} moved out, {HouseholdMemberCount} household members deactivated, tenant {TenantId}",
            id, registrationMembers.Count, tenantId);

        return registration.ToDto();
    }
}
