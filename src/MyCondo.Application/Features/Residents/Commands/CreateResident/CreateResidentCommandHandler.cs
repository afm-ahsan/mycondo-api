using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Residents.DTOs;
using MyCondo.Application.Features.Residents.Mappings;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Property.Flats;
using MyCondo.Domain.Features.Residents;

namespace MyCondo.Application.Features.Residents.Commands.CreateResident;

public sealed class CreateResidentCommandHandler(
    IResidentRepository residents,
    IFlatRepository flats,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    IClock clock,
    ILogger<CreateResidentCommandHandler> logger
) : IRequestHandler<CreateResidentCommand, ResidentDto>
{
    public async ValueTask<ResidentDto> Handle(CreateResidentCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        FlatId flatId = new(command.FlatId);
        Flat flat = await flats.GetByIdAsync(flatId, cancellationToken)
            ?? throw new NotFoundException(nameof(Flat), command.FlatId);

        if (flat.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(Flat), command.FlatId);
        }

        ResidentType residentType = Enum.Parse<ResidentType>(command.ResidentType);
        Resident resident = Resident.Register(
            tenantId, flatId, command.FullName, command.Phone, command.Email, residentType, clock.UtcNow);

        residents.Add(resident);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Resident {ResidentId} '{FullName}' registered for flat {FlatId}, tenant {TenantId}",
            resident.Id, resident.FullName, flatId, tenantId);

        return resident.ToDto();
    }
}
