using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Operations.DTOs;
using MyCondo.Application.Features.Operations.Mappings;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Operations.GeneratorBreakdownRecords;

namespace MyCondo.Application.Features.Operations.Commands.ResolveBreakdown;

public sealed class ResolveBreakdownCommandHandler(
    IGeneratorBreakdownRecordRepository breakdowns,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    ILogger<ResolveBreakdownCommandHandler> logger
) : IRequestHandler<ResolveBreakdownCommand, GeneratorBreakdownRecordDto>
{
    public async ValueTask<GeneratorBreakdownRecordDto> Handle(ResolveBreakdownCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        GeneratorBreakdownRecordId id = new(command.GeneratorBreakdownRecordId);
        GeneratorBreakdownRecord record = await breakdowns.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(GeneratorBreakdownRecord), command.GeneratorBreakdownRecordId);
        if (record.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(GeneratorBreakdownRecord), command.GeneratorBreakdownRecordId);
        }

        record.Resolve(command.Resolution, command.Cost, command.DowntimeEndUtc);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Breakdown {GeneratorBreakdownRecordId} resolved, tenant {TenantId}", id, tenantId);

        return record.ToDto();
    }
}
