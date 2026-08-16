using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Leasing.DTOs;
using MyCondo.Application.Features.Leasing.Mappings;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Leasing.HouseholdMembers;

namespace MyCondo.Application.Features.Leasing.Commands.UpdateHouseholdMember;

public sealed class UpdateHouseholdMemberCommandHandler(
    IHouseholdMemberRepository members,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    IClock clock,
    ILogger<UpdateHouseholdMemberCommandHandler> logger
) : IRequestHandler<UpdateHouseholdMemberCommand, HouseholdMemberDto>
{
    public async ValueTask<HouseholdMemberDto> Handle(UpdateHouseholdMemberCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        HouseholdMemberId id = new(command.HouseholdMemberId);
        HouseholdMember member = await members.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(HouseholdMember), command.HouseholdMemberId);
        if (member.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(HouseholdMember), command.HouseholdMemberId);
        }

        member.Update(
            command.FullName, command.RelationshipToPrimary, command.DateOfBirth, command.Phone,
            command.NationalIdNumber, command.Gender, command.BirthCertificateNumber, command.BloodGroup,
            command.Religion, command.Nationality, command.Occupation, clock.UtcNow);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Household member {HouseholdMemberId} updated, tenant {TenantId}", id, tenantId);

        return member.ToDto();
    }
}
