using Mediator;
using MyCondo.Application.Features.Billing.DTOs;

namespace MyCondo.Application.Features.Billing.Commands.CreateServiceChargeRule;

public sealed record CreateServiceChargeRuleCommand(
    Guid BuildingId,
    string Category,
    string Name,
    string CalculationMethod,
    decimal Rate,
    string? UnitTypeFilter,
    string Frequency,
    DateOnly EffectiveFrom
) : IRequest<ServiceChargeRuleDto>;
