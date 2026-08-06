using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Utilities.DTOs;
using MyCondo.Application.Features.Utilities.Mappings;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Domain.Features.Utilities.Common;
using MyCondo.Domain.Features.Utilities.Meters;

namespace MyCondo.Application.Features.Utilities.Commands.InstallMeter;

public sealed class InstallMeterCommandHandler(
    IMeterRepository meters,
    IBuildingRepository buildings,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    IClock clock,
    ILogger<InstallMeterCommandHandler> logger
) : IRequestHandler<InstallMeterCommand, MeterDto>
{
    public async ValueTask<MeterDto> Handle(InstallMeterCommand command, CancellationToken cancellationToken)
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

        UtilityType utilityType = Enum.Parse<UtilityType>(command.UtilityType);
        string meterNumber = command.MeterNumber.Trim();

        Meter? existing = await meters.GetByMeterNumberAsync(tenantId, utilityType, meterNumber, cancellationToken);
        if (existing is not null)
        {
            throw new ConflictException($"A {utilityType} meter with number '{meterNumber}' already exists for this tenant.");
        }

        Meter meter = Meter.Install(tenantId, buildingId, utilityType, meterNumber, clock.UtcNow);
        meters.Add(meter);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Meter {MeterId} '{MeterNumber}' ({UtilityType}) installed for building {BuildingId}, tenant {TenantId}",
            meter.Id, meterNumber, utilityType, buildingId, tenantId);

        return meter.ToDto();
    }
}
