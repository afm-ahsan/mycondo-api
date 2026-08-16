using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Leasing.DTOs;
using MyCondo.Application.Features.Leasing.Mappings;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Leasing.HouseholdMembers;
using MyCondo.Domain.Features.Leasing.OccupancyRegistrations;

namespace MyCondo.Application.Features.Leasing.Commands.AddHouseholdMember;

public sealed class AddHouseholdMemberCommandHandler(
    IOccupancyRegistrationRepository registrations,
    IHouseholdMemberRepository members,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    IClock clock,
    ILogger<AddHouseholdMemberCommandHandler> logger
) : IRequestHandler<AddHouseholdMemberCommand, HouseholdMemberDto>
{
    public async ValueTask<HouseholdMemberDto> Handle(AddHouseholdMemberCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        OccupancyRegistrationId registrationId = new(command.OccupancyRegistrationId);
        OccupancyRegistration registration = await registrations.GetByIdAsync(registrationId, cancellationToken)
            ?? throw new NotFoundException(nameof(OccupancyRegistration), command.OccupancyRegistrationId);
        if (registration.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(OccupancyRegistration), command.OccupancyRegistrationId);
        }

        if (registration.Status is OccupancyRegistrationStatus.Rejected or OccupancyRegistrationStatus.MovedOut)
        {
            throw new ConflictException($"Cannot add a household member while the registration is {registration.Status}.");
        }

        HouseholdMember member = HouseholdMember.Add(
            tenantId, registrationId, command.FullName, command.RelationshipToPrimary, command.DateOfBirth,
            command.Phone, command.NationalIdNumber, command.Gender, command.BirthCertificateNumber,
            command.BloodGroup, command.Religion, command.Nationality, command.Occupation, clock.UtcNow);

        members.Add(member);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Household member {HouseholdMemberId} added to occupancy registration {OccupancyRegistrationId}, tenant {TenantId}",
            member.Id, registrationId, tenantId);

        return member.ToDto();
    }
}
