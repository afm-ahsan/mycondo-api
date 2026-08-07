using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Operations.DTOs;
using MyCondo.Application.Features.Operations.Mappings;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Operations.CylinderStockMovements;
using MyCondo.Domain.Features.Operations.MonthlyCylinderReconciliations;

namespace MyCondo.Application.Features.Operations.Commands.CreateMonthlyReconciliation;

/// <summary>
/// Computes opening/closing stock and period totals from the <see cref="CylinderStockMovement"/>
/// ledger — this handler owns the aggregation logic (the entity itself only records the frozen
/// snapshot, see its own doc comment). "Variance" (register-digitization spec §5.14) is the gap
/// between the ledger's expected closing stock and the actual counted closing stock, computed by the
/// entity's own <c>Create</c> factory from the numbers passed in here.
/// </summary>
public sealed class CreateMonthlyReconciliationCommandHandler(
    ICylinderStockMovementRepository movements,
    IMonthlyCylinderReconciliationRepository reconciliations,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    IClock clock,
    ILogger<CreateMonthlyReconciliationCommandHandler> logger
) : IRequestHandler<CreateMonthlyReconciliationCommand, MonthlyCylinderReconciliationDto>
{
    public async ValueTask<MonthlyCylinderReconciliationDto> Handle(
        CreateMonthlyReconciliationCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        DateTimeOffset periodStartUtc = new(command.PeriodMonth.Year, command.PeriodMonth.Month, 1, 0, 0, 0, TimeSpan.Zero);
        DateTimeOffset periodEndUtc = periodStartUtc.AddMonths(1);

        IReadOnlyList<CylinderStockMovement> allMovementsUpToPeriodEnd = await movements.GetForPeriodAsync(
            tenantId, command.CylinderType, DateTimeOffset.MinValue, periodEndUtc.AddTicks(-1), cancellationToken);

        int openingStock = allMovementsUpToPeriodEnd
            .Where(m => m.OccurredAtUtc < periodStartUtc)
            .Sum(m => m.Quantity);

        List<CylinderStockMovement> periodMovements = [.. allMovementsUpToPeriodEnd.Where(m => m.OccurredAtUtc >= periodStartUtc)];

        int totalReceived = periodMovements.Where(m => m.MovementType == CylinderStockMovementType.Receipt).Sum(m => m.Quantity);
        int totalIssued = -periodMovements.Where(m => m.MovementType == CylinderStockMovementType.Issue).Sum(m => m.Quantity);
        int totalEmptyReturned = -periodMovements.Where(m => m.MovementType == CylinderStockMovementType.EmptyReturn).Sum(m => m.Quantity);
        int actualClosingStock = openingStock + periodMovements.Sum(m => m.Quantity);

        MonthlyCylinderReconciliation reconciliation = MonthlyCylinderReconciliation.Create(
            tenantId, command.CylinderType, command.PeriodMonth, openingStock, totalReceived, totalIssued, totalEmptyReturned,
            actualClosingStock, command.Remarks, currentUser.UserId, clock.UtcNow);

        reconciliations.Add(reconciliation);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Monthly cylinder reconciliation {MonthlyCylinderReconciliationId} created for {CylinderType} {PeriodMonth}, tenant {TenantId}",
            reconciliation.Id, command.CylinderType, command.PeriodMonth, tenantId);

        return reconciliation.ToDto();
    }
}
