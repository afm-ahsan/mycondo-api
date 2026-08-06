using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Property.Flats.DTOs;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Property.Flats;

namespace MyCondo.Application.Features.Property.Flats.Commands.UpdateFlatArea;

public sealed class UpdateFlatAreaCommandHandler(
    IFlatRepository flats,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    ILogger<UpdateFlatAreaCommandHandler> logger
) : IRequestHandler<UpdateFlatAreaCommand, FlatDto>
{
    public async ValueTask<FlatDto> Handle(UpdateFlatAreaCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        FlatId flatId = new(command.FlatId);
        Flat flat = await flats.GetByIdAsync(flatId, cancellationToken)
            ?? throw new NotFoundException(nameof(Flat), command.FlatId);
        if (flat.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(Flat), command.FlatId);
        }

        flat.SetAreaSqFt(command.AreaSqFt);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Flat {FlatId} area set to {AreaSqFt}, tenant {TenantId}", flatId, command.AreaSqFt, tenantId);

        return new FlatDto(flat.Id.Value, flat.BuildingId.Value, flat.FlatNumber, flat.FloorNumber, flat.FlatType.ToString(), flat.AreaSqFt);
    }
}
