using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Operations.DTOs;
using MyCondo.Application.Features.Operations.Mappings;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Operations.GeneratorBreakdownRecords;
using MyCondo.Domain.Features.Operations.Generators;

namespace MyCondo.Application.Features.Operations.Commands.RecordBreakdown;

public sealed class RecordBreakdownCommandHandler(
    IGeneratorBreakdownRecordRepository breakdowns,
    IGeneratorRepository generators,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    IClock clock,
    ILogger<RecordBreakdownCommandHandler> logger
) : IRequestHandler<RecordBreakdownCommand, GeneratorBreakdownRecordDto>
{
    public async ValueTask<GeneratorBreakdownRecordDto> Handle(RecordBreakdownCommand command, CancellationToken cancellationToken)
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

        GeneratorBreakdownRecord record = GeneratorBreakdownRecord.Report(
            tenantId, generatorId, command.ReportedAtUtc, command.Description, command.DowntimeStartUtc, clock.UtcNow);

        breakdowns.Add(record);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Breakdown {GeneratorBreakdownRecordId} reported for generator {GeneratorId}, tenant {TenantId}",
            record.Id, generatorId, tenantId);

        return record.ToDto();
    }
}
