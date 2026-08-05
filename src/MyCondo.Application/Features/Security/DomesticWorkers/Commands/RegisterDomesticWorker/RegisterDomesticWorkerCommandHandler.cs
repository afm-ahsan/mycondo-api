using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Security.DomesticWorkers.DTOs;
using MyCondo.Application.Features.Security.DomesticWorkers.Mappings;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Security.DomesticWorkers;

namespace MyCondo.Application.Features.Security.DomesticWorkers.Commands.RegisterDomesticWorker;

public sealed class RegisterDomesticWorkerCommandHandler(
    IDomesticWorkerProfileRepository profiles,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    IClock clock,
    ILogger<RegisterDomesticWorkerCommandHandler> logger
) : IRequestHandler<RegisterDomesticWorkerCommand, DomesticWorkerProfileDto>
{
    public async ValueTask<DomesticWorkerProfileDto> Handle(RegisterDomesticWorkerCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        DomesticWorkerType workerType = Enum.Parse<DomesticWorkerType>(command.WorkerType);

        DomesticWorkerProfile profile = DomesticWorkerProfile.Register(
            tenantId, command.FullName, command.Phone, workerType, command.IdentityDocumentType,
            command.IdentityDocumentNumber, command.EmergencyContactName, command.EmergencyContactPhone,
            clock.UtcNow);

        profiles.Add(profile);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Domestic worker profile {ProfileId} '{FullName}' registered for tenant {TenantId}",
            profile.Id, profile.FullName, tenantId);

        return profile.ToDto();
    }
}
