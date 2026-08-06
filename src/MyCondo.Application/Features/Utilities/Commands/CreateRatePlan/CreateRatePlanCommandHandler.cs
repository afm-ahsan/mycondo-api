using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Utilities.DTOs;
using MyCondo.Application.Features.Utilities.Mappings;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Domain.Features.Utilities.Common;
using MyCondo.Domain.Features.Utilities.RatePlans;

namespace MyCondo.Application.Features.Utilities.Commands.CreateRatePlan;

public sealed class CreateRatePlanCommandHandler(
    IRatePlanRepository ratePlans,
    IBuildingRepository buildings,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    IClock clock,
    ILogger<CreateRatePlanCommandHandler> logger
) : IRequestHandler<CreateRatePlanCommand, RatePlanDto>
{
    public async ValueTask<RatePlanDto> Handle(CreateRatePlanCommand command, CancellationToken cancellationToken)
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

        UtilityType utilityType = Enum.Parse<UtilityType>(command.UtilityType);
        RateStructure structure = Enum.Parse<RateStructure>(command.Structure);

        bool overlaps = await ratePlans.HasOverlappingPlanAsync(
            tenantId, buildingId, utilityType, command.EffectiveFrom, null, cancellationToken);
        if (overlaps)
        {
            throw new ConflictException(
                $"An effective rate plan already covers this building/utility-type combination on or after {command.EffectiveFrom}.");
        }

        IReadOnlyList<RateSlabInput> slabInputs = command.Slabs
            .Select(s => new RateSlabInput(s.SlabOrder, s.FromUnits, s.ToUnits, s.RatePerUnit))
            .ToList();

        (RatePlan plan, IReadOnlyList<RateSlab> slabs) = RatePlan.Create(
            tenantId, buildingId, utilityType, command.Name, structure, command.FixedAmount,
            command.FixedServiceCharge, command.TaxPercentage, command.EffectiveFrom, slabInputs, clock.UtcNow);

        ratePlans.Add(plan);
        ratePlans.AddSlabs(slabs);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Rate plan {RatePlanId} '{Name}' created for building {BuildingId}, tenant {TenantId}",
            plan.Id, plan.Name, buildingId, tenantId);

        return plan.ToDto(slabs);
    }
}
