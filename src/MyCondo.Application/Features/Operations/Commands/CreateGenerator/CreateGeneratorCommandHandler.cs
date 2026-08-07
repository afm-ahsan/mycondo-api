using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Operations.DTOs;
using MyCondo.Application.Features.Operations.Mappings;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Operations.Generators;
using MyCondo.Domain.Features.Property.Buildings;

namespace MyCondo.Application.Features.Operations.Commands.CreateGenerator;

public sealed class CreateGeneratorCommandHandler(
    IGeneratorRepository generators,
    IBuildingRepository buildings,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    IClock clock,
    ILogger<CreateGeneratorCommandHandler> logger
) : IRequestHandler<CreateGeneratorCommand, GeneratorDto>
{
    public async ValueTask<GeneratorDto> Handle(CreateGeneratorCommand command, CancellationToken cancellationToken)
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

        Generator generator = Generator.Create(
            tenantId, buildingId, command.Name, command.Model, command.CapacityKva, command.Location, clock.UtcNow);

        generators.Add(generator);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Generator {GeneratorId} ('{Name}') created for tenant {TenantId}", generator.Id, generator.Name, tenantId);

        return generator.ToDto();
    }
}
