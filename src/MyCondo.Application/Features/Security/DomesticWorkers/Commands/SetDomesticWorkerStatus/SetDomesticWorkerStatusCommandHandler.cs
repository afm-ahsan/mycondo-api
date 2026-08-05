using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Security.DomesticWorkers.DTOs;
using MyCondo.Application.Features.Security.DomesticWorkers.Mappings;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Security.Common;
using MyCondo.Domain.Features.Security.DomesticWorkers;

namespace MyCondo.Application.Features.Security.DomesticWorkers.Commands.SetDomesticWorkerStatus;

public sealed class SetDomesticWorkerStatusCommandHandler(
    IDomesticWorkerProfileRepository profiles,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    ILogger<SetDomesticWorkerStatusCommandHandler> logger
) : IRequestHandler<SetDomesticWorkerStatusCommand, DomesticWorkerProfileDto>
{
    public async ValueTask<DomesticWorkerProfileDto> Handle(SetDomesticWorkerStatusCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        DomesticWorkerProfileId id = new(command.DomesticWorkerProfileId);
        DomesticWorkerProfile profile = await profiles.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(DomesticWorkerProfile), command.DomesticWorkerProfileId);

        if (profile.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(DomesticWorkerProfile), command.DomesticWorkerProfileId);
        }

        RecurringAccessProfileStatus status = Enum.Parse<RecurringAccessProfileStatus>(command.Status);
        switch (status)
        {
            case RecurringAccessProfileStatus.Active:
                profile.Reactivate();
                break;
            case RecurringAccessProfileStatus.Suspended:
                profile.Suspend(command.Reason!);
                break;
            case RecurringAccessProfileStatus.Blocked:
                profile.Block(command.Reason!);
                break;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Domestic worker profile {ProfileId} status set to {Status} for tenant {TenantId}",
            id, status, tenantId);

        return profile.ToDto();
    }
}
