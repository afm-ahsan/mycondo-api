using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Billing.DTOs;

namespace MyCondo.Application.Features.Billing.Commands.DeactivateServiceChargeRule;

public sealed record DeactivateServiceChargeRuleCommand(Guid ServiceChargeRuleId) : IRequest<ServiceChargeRuleDto>, ILifecycleWriteOperation;
