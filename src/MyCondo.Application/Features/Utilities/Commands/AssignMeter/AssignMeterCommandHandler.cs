using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Utilities.DTOs;
using MyCondo.Application.Features.Utilities.Mappings;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Property.Flats;
using MyCondo.Domain.Features.Utilities.MeterAssignments;
using MyCondo.Domain.Features.Utilities.Meters;

namespace MyCondo.Application.Features.Utilities.Commands.AssignMeter;

public sealed class AssignMeterCommandHandler(
    IMeterRepository meters,
    IMeterAssignmentRepository assignments,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    IClock clock,
    ILogger<AssignMeterCommandHandler> logger
) : IRequestHandler<AssignMeterCommand, MeterAssignmentDto>
{
    public async ValueTask<MeterAssignmentDto> Handle(AssignMeterCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        MeterId meterId = new(command.MeterId);
        Meter meter = await meters.GetByIdAsync(meterId, cancellationToken)
            ?? throw new NotFoundException(nameof(Meter), command.MeterId);
        if (meter.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(Meter), command.MeterId);
        }

        DateTimeOffset nowUtc = clock.UtcNow;

        MeterAssignment? open = await assignments.GetOpenForMeterAsync(tenantId, meterId, cancellationToken);
        open?.EndAssignment(nowUtc);

        MeterAssignment assignment = MeterAssignment.Assign(tenantId, meterId, new FlatId(command.FlatId), nowUtc);
        assignments.Add(assignment);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Meter {MeterId} assigned to flat {FlatId}, tenant {TenantId}", meterId, command.FlatId, tenantId);

        return assignment.ToDto();
    }
}
