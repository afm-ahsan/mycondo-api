using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Operations.DTOs;
using MyCondo.Application.Features.Operations.Mappings;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Operations.GeneratorSessions;
using MyCondo.Domain.Features.Operations.Generators;

namespace MyCondo.Application.Features.Operations.Commands.StopGeneratorSession;

public sealed class StopGeneratorSessionCommandHandler(
    IGeneratorSessionRepository sessions,
    IGeneratorRepository generators,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    IClock clock,
    ILogger<StopGeneratorSessionCommandHandler> logger
) : IRequestHandler<StopGeneratorSessionCommand, GeneratorSessionDto>
{
    public async ValueTask<GeneratorSessionDto> Handle(StopGeneratorSessionCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        GeneratorSessionId id = new(command.GeneratorSessionId);
        GeneratorSession session = await sessions.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(GeneratorSession), command.GeneratorSessionId);
        if (session.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(GeneratorSession), command.GeneratorSessionId);
        }

        session.Stop(command.ClosingFuelLevel, command.OutageReason, clock.UtcNow);

        if (command.HourMeterReading is decimal newReading)
        {
            Generator generator = await generators.GetByIdAsync(session.GeneratorId, cancellationToken)
                ?? throw new NotFoundException(nameof(Generator), session.GeneratorId.Value);
            generator.AdvanceHourMeter(newReading);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Generator session {GeneratorSessionId} stopped, runtime {RuntimeMinutes} minutes, tenant {TenantId}",
            session.Id, session.RuntimeMinutes, tenantId);

        return session.ToDto();
    }
}
