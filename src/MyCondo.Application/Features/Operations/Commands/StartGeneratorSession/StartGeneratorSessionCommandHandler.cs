using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Operations.DTOs;
using MyCondo.Application.Features.Operations.Mappings;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Operations.GeneratorSessions;
using MyCondo.Domain.Features.Operations.GeneratorSessions.Exceptions;
using MyCondo.Domain.Features.Operations.Generators;
using MyCondo.Domain.Features.Operations.Generators.Exceptions;

namespace MyCondo.Application.Features.Operations.Commands.StartGeneratorSession;

/// <summary>
/// "Only one open session per generator" (register-digitization spec §5.13) is enforced by locking
/// the <see cref="Generator"/> row for the duration of this transaction before checking for an
/// existing open session — the exact pattern <c>CheckInPoolSessionCommandHandler</c> uses for pool
/// capacity (Slice G), added there specifically because a plain read-then-check-then-write sequence
/// can race two concurrent requests past the check before either commits.
/// </summary>
public sealed class StartGeneratorSessionCommandHandler(
    IGeneratorRepository generators,
    IGeneratorSessionRepository sessions,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    IClock clock,
    ILogger<StartGeneratorSessionCommandHandler> logger
) : IRequestHandler<StartGeneratorSessionCommand, GeneratorSessionDto>
{
    public async ValueTask<GeneratorSessionDto> Handle(StartGeneratorSessionCommand command, CancellationToken cancellationToken)
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

        if (!generator.IsActive)
        {
            throw new GeneratorInactiveException(generatorId);
        }

        await using IUnitOfWorkTransaction transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        await generators.LockForSessionStartCheckAsync(generatorId, cancellationToken);

        GeneratorSession? openSession = await sessions.GetOpenForGeneratorAsync(tenantId, generatorId, cancellationToken);
        if (openSession is not null)
        {
            throw new GeneratorAlreadyHasOpenSessionException(generatorId);
        }

        GeneratorSession session = GeneratorSession.Start(
            tenantId, generatorId, currentUser.UserId, command.OpeningFuelLevel, clock.UtcNow);

        sessions.Add(session);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        logger.LogInformation(
            "Generator session {GeneratorSessionId} started for generator {GeneratorId}, tenant {TenantId}",
            session.Id, generatorId, tenantId);

        return session.ToDto();
    }
}
