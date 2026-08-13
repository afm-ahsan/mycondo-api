using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Property.Flats.DTOs;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Property.Flats;

namespace MyCondo.Application.Features.Property.Flats.Commands.UpdateFlat;

public sealed class UpdateFlatCommandHandler(
    IFlatRepository flats,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    ILogger<UpdateFlatCommandHandler> logger
) : IRequestHandler<UpdateFlatCommand, FlatDto>
{
    public async ValueTask<FlatDto> Handle(UpdateFlatCommand command, CancellationToken cancellationToken)
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

        string flatNumber = command.FlatNumber.Trim();
        Flat? existing = await flats.GetByFlatNumberAsync(tenantId, flat.BuildingId, flatNumber, cancellationToken);
        if (existing is not null && existing.Id != flatId)
        {
            throw new ConflictException($"Flat '{flatNumber}' already exists in this building.");
        }

        FlatType flatType = Enum.Parse<FlatType>(command.FlatType);
        flat.UpdateDetails(flatNumber, command.FloorNumber, flatType);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Flat {FlatId} updated for tenant {TenantId}", flatId, tenantId);

        return new FlatDto(flat.Id.Value, flat.BuildingId.Value, flat.FlatNumber, flat.FloorNumber, flat.FlatType.ToString(), flat.AreaSqFt, flat.PrimaryPhotoAttachmentId);
    }
}
