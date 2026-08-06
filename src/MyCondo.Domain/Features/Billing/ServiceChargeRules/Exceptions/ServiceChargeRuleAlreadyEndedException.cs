using MyCondo.Domain.Exceptions;

namespace MyCondo.Domain.Features.Billing.ServiceChargeRules.Exceptions;

public sealed class ServiceChargeRuleAlreadyEndedException(ServiceChargeRuleId ruleId)
    : DomainException($"Service charge rule {ruleId} already has an EffectiveTo date set.")
{
    public ServiceChargeRuleId RuleId { get; } = ruleId;
}
