using Mediator;
using MyCondo.Application.Features.Billing.DTOs;

namespace MyCondo.Application.Features.Billing.Commands.EndServiceChargeRulePeriod;

public sealed record EndServiceChargeRulePeriodCommand(Guid ServiceChargeRuleId, DateOnly EffectiveTo) : IRequest<ServiceChargeRuleDto>;
