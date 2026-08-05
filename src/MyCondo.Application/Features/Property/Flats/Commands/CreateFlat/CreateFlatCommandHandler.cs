using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Property.Flats.DTOs;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Domain.Features.Property.Flats;

namespace MyCondo.Application.Features.Property.Flats.Commands.CreateFlat;

public sealed class CreateFlatCommandHandler(
    IFlatRepository flats,
    IBuildingRepository buildings,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    IClock clock,
    ILogger<CreateFlatCommandHandler> logger
) : IRequestHandler<CreateFlatCommand, FlatDto>
{
    public async ValueTask<FlatDto> Handle(CreateFlatCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        BuildingId buildingId = new(command.BuildingId);
        Building building = await buildings.GetByIdAsync(buildingId, cancellationToken)
            ?? throw new NotFoundException(nameof(Building), command.BuildingId);

        if (building.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(Building), command.BuildingId);
        }

        string flatNumber = command.FlatNumber.Trim();
        Flat? existing = await flats.GetByFlatNumberAsync(tenantId, buildingId, flatNumber, cancellationToken);
        if (existing is not null)
        {
            throw new ConflictException($"Flat '{flatNumber}' already exists in this building.");
        }

        FlatType flatType = Enum.Parse<FlatType>(command.FlatType);
        Flat flat = Flat.Create(tenantId, buildingId, flatNumber, command.FloorNumber, flatType, clock.UtcNow);

        flats.Add(flat);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Flat {FlatId} '{FlatNumber}' created in building {BuildingId} for tenant {TenantId}",
            flat.Id, flat.FlatNumber, buildingId, tenantId);

        return new FlatDto(flat.Id.Value, flat.BuildingId.Value, flat.FlatNumber, flat.FloorNumber, flat.FlatType.ToString());
    }
}
