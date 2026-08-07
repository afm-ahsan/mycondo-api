using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Operations.DTOs;
using MyCondo.Application.Features.Operations.Mappings;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Operations.Generators;

namespace MyCondo.Application.Features.Operations.Commands.UpdateGenerator;

public sealed class UpdateGeneratorCommandHandler(
    IGeneratorRepository generators,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    ILogger<UpdateGeneratorCommandHandler> logger
) : IRequestHandler<UpdateGeneratorCommand, GeneratorDto>
{
    public async ValueTask<GeneratorDto> Handle(UpdateGeneratorCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        GeneratorId generatorId = new(command.GeneratorId);
        Generator generator = await generators.GetByIdAsync(generatorId, cancellationToken)
            ?? throw new NotFoundException(nameof(Generator), command.GeneratorId);
        if (generator.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(Generator), command.GeneratorId);
        }

        generator.UpdateDetails(command.Name, command.Model, command.CapacityKva, command.Location);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Generator {GeneratorId} updated for tenant {TenantId}", generator.Id, tenantId);

        return generator.ToDto();
    }
}
