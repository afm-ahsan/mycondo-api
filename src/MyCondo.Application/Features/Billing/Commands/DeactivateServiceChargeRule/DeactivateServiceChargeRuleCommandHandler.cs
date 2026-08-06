using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Billing.DTOs;
using MyCondo.Application.Features.Billing.Mappings;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Billing.ServiceChargeRules;

namespace MyCondo.Application.Features.Billing.Commands.DeactivateServiceChargeRule;

public sealed class DeactivateServiceChargeRuleCommandHandler(
    IServiceChargeRuleRepository rules,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    ILogger<DeactivateServiceChargeRuleCommandHandler> logger
) : IRequestHandler<DeactivateServiceChargeRuleCommand, ServiceChargeRuleDto>
{
    public async ValueTask<ServiceChargeRuleDto> Handle(DeactivateServiceChargeRuleCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        ServiceChargeRuleId id = new(command.ServiceChargeRuleId);
        ServiceChargeRule rule = await rules.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(ServiceChargeRule), command.ServiceChargeRuleId);
        if (rule.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(ServiceChargeRule), command.ServiceChargeRuleId);
        }

        rule.Deactivate();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Service charge rule {RuleId} deactivated, tenant {TenantId}", id, tenantId);

        return rule.ToDto();
    }
}
