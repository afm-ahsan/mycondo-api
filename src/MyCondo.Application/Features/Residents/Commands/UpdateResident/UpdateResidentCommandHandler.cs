using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Residents.DTOs;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Residents;

namespace MyCondo.Application.Features.Residents.Commands.UpdateResident;

public sealed class UpdateResidentCommandHandler(
    IResidentRepository residents,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    IClock clock,
    ILogger<UpdateResidentCommandHandler> logger
) : IRequestHandler<UpdateResidentCommand, ResidentDto>
{
    public async ValueTask<ResidentDto> Handle(UpdateResidentCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        ResidentId residentId = new(command.ResidentId);
        Resident resident = await residents.GetByIdAsync(residentId, cancellationToken)
            ?? throw new NotFoundException(nameof(Resident), command.ResidentId);

        if (resident.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(Resident), command.ResidentId);
        }

        resident.UpdateProfile(command.FullName, command.Phone, command.Email, clock.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Resident {ResidentId} updated for tenant {TenantId}", residentId, tenantId);

        return new ResidentDto(
            resident.Id.Value, resident.FlatId.Value, resident.FullName, resident.Phone, resident.Email,
            resident.ResidentType.ToString());
    }
}
