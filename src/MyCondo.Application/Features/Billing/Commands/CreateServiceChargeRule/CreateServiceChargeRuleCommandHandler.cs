using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Billing.DTOs;
using MyCondo.Application.Features.Billing.Mappings;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Billing.ServiceChargeRules;
using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Domain.Features.Property.Flats;

namespace MyCondo.Application.Features.Billing.Commands.CreateServiceChargeRule;

public sealed class CreateServiceChargeRuleCommandHandler(
    IServiceChargeRuleRepository rules,
    IBuildingRepository buildings,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    IClock clock,
    ILogger<CreateServiceChargeRuleCommandHandler> logger
) : IRequestHandler<CreateServiceChargeRuleCommand, ServiceChargeRuleDto>
{
    public async ValueTask<ServiceChargeRuleDto> Handle(CreateServiceChargeRuleCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        BuildingId buildingId = new(command.BuildingId);
        Building building = await buildings.GetByIdAsync(buildingId, cancellationToken)
            ?? throw new NotFoundException(nameof(Building), command.BuildingId);
        if (building.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(Building), command.BuildingId);
        }

        CalculationMethod calculationMethod = Enum.Parse<CalculationMethod>(command.CalculationMethod);
        BillingFrequency frequency = Enum.Parse<BillingFrequency>(command.Frequency);
        FlatType? unitTypeFilter = command.UnitTypeFilter is null ? null : Enum.Parse<FlatType>(command.UnitTypeFilter);
        string category = command.Category.Trim();

        bool overlaps = await rules.HasOverlappingRuleAsync(
            tenantId, buildingId, category, unitTypeFilter, frequency, command.EffectiveFrom, null, cancellationToken);
        if (overlaps)
        {
            throw new ConflictException(
                $"An effective rule already covers this building/category/unit-type/frequency combination on or after {command.EffectiveFrom}.");
        }

        ServiceChargeRule rule = ServiceChargeRule.Create(
            tenantId, buildingId, category, command.Name, calculationMethod, command.Rate, unitTypeFilter, frequency,
            command.EffectiveFrom, clock.UtcNow);

        rules.Add(rule);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Service charge rule {RuleId} '{Name}' created for building {BuildingId}, tenant {TenantId}",
            rule.Id, rule.Name, buildingId, tenantId);

        return rule.ToDto();
    }
}
