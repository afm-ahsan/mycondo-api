using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Security.DomesticWorkerAssignments.DTOs;
using MyCondo.Application.Features.Security.DomesticWorkerAssignments.Mappings;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Property.Flats;
using MyCondo.Domain.Features.Security.Common;
using MyCondo.Domain.Features.Security.DomesticWorkerAssignments;
using MyCondo.Domain.Features.Security.DomesticWorkers;

namespace MyCondo.Application.Features.Security.DomesticWorkerAssignments.Commands.CreateDomesticWorkerAssignment;

public sealed class CreateDomesticWorkerAssignmentCommandHandler(
    IDomesticWorkerAssignmentRepository assignments,
    IDomesticWorkerProfileRepository profiles,
    IFlatRepository flats,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    IClock clock,
    ILogger<CreateDomesticWorkerAssignmentCommandHandler> logger
) : IRequestHandler<CreateDomesticWorkerAssignmentCommand, DomesticWorkerAssignmentDto>
{
    public async ValueTask<DomesticWorkerAssignmentDto> Handle(CreateDomesticWorkerAssignmentCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        DomesticWorkerProfileId workerId = new(command.DomesticWorkerProfileId);
        DomesticWorkerProfile worker = await profiles.GetByIdAsync(workerId, cancellationToken)
            ?? throw new NotFoundException(nameof(DomesticWorkerProfile), command.DomesticWorkerProfileId);
        if (worker.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(DomesticWorkerProfile), command.DomesticWorkerProfileId);
        }

        FlatId flatId = new(command.FlatId);
        Flat flat = await flats.GetByIdAsync(flatId, cancellationToken)
            ?? throw new NotFoundException(nameof(Flat), command.FlatId);
        if (flat.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(Flat), command.FlatId);
        }

        DaysOfWeekFlags allowedDays = string.IsNullOrWhiteSpace(command.AllowedDays)
            ? DaysOfWeekFlags.All
            : Enum.Parse<DaysOfWeekFlags>(command.AllowedDays);

        DomesticWorkerAssignment assignment = DomesticWorkerAssignment.Create(
            tenantId, workerId, flatId, command.ValidFromUtc, command.ValidToUtc, allowedDays,
            command.AllowedStartTime, command.AllowedEndTime, clock.UtcNow);

        assignments.Add(assignment);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Domestic worker assignment {AssignmentId} created for worker {WorkerId}, flat {FlatId}, tenant {TenantId}",
            assignment.Id, workerId, flatId, tenantId);

        return assignment.ToDto();
    }
}
