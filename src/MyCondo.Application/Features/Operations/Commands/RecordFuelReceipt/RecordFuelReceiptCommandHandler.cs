using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Operations.DTOs;
using MyCondo.Application.Features.Operations.Mappings;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Operations.GeneratorFuelReceipts;
using MyCondo.Domain.Features.Operations.Generators;

namespace MyCondo.Application.Features.Operations.Commands.RecordFuelReceipt;

public sealed class RecordFuelReceiptCommandHandler(
    IGeneratorFuelReceiptRepository fuelReceipts,
    IGeneratorRepository generators,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    IClock clock,
    ILogger<RecordFuelReceiptCommandHandler> logger
) : IRequestHandler<RecordFuelReceiptCommand, GeneratorFuelReceiptDto>
{
    public async ValueTask<GeneratorFuelReceiptDto> Handle(RecordFuelReceiptCommand command, CancellationToken cancellationToken)
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

        GeneratorFuelReceipt receipt = GeneratorFuelReceipt.Record(
            tenantId, generatorId, command.ReceivedAtUtc, command.Quantity, command.Cost, command.Supplier,
            command.Remarks, clock.UtcNow);

        fuelReceipts.Add(receipt);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Fuel receipt {GeneratorFuelReceiptId} recorded for generator {GeneratorId}, tenant {TenantId}",
            receipt.Id, generatorId, tenantId);

        return receipt.ToDto();
    }
}
