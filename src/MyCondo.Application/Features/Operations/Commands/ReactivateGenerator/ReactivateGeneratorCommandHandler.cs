using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Operations.DTOs;
using MyCondo.Application.Features.Operations.Mappings;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Operations.Generators;

namespace MyCondo.Application.Features.Operations.Commands.ReactivateGenerator;

public sealed class ReactivateGeneratorCommandHandler(
    IGeneratorRepository generators,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    ILogger<ReactivateGeneratorCommandHandler> logger
) : IRequestHandler<ReactivateGeneratorCommand, GeneratorDto>
{
    public async ValueTask<GeneratorDto> Handle(ReactivateGeneratorCommand command, CancellationToken cancellationToken)
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

        generator.Reactivate();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Generator {GeneratorId} reactivated, tenant {TenantId}", id, tenantId);

        return generator.ToDto();
    }
}
