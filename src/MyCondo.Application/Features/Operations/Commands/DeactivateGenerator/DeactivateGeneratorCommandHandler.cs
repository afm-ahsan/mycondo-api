using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Operations.DTOs;
using MyCondo.Application.Features.Operations.Mappings;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Operations.Generators;

namespace MyCondo.Application.Features.Operations.Commands.DeactivateGenerator;

public sealed class DeactivateGeneratorCommandHandler(
    IGeneratorRepository generators,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    ILogger<DeactivateGeneratorCommandHandler> logger
) : IRequestHandler<DeactivateGeneratorCommand, GeneratorDto>
{
    public async ValueTask<GeneratorDto> Handle(DeactivateGeneratorCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        GeneratorId id = new(command.GeneratorId);
        Generator generator = await generators.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(Generator), command.GeneratorId);
        if (generator.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(Generator), command.GeneratorId);
        }

        generator.Deactivate();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Generator {GeneratorId} deactivated, tenant {TenantId}", id, tenantId);

        return generator.ToDto();
    }
}
