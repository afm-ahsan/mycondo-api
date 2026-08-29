using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Operations.DTOs;

namespace MyCondo.Application.Features.Operations.Commands.CreateMonthlyReconciliation;

public sealed record CreateMonthlyReconciliationCommand(
    string CylinderType,
    DateOnly PeriodMonth,
    string? Remarks
) : IRequest<MonthlyCylinderReconciliationDto>, IRequiresFeature, ILifecycleWriteOperation
{
    public string FeatureKey => "operations.gas_cylinders";
}
