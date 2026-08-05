using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Security.ServiceProviderAssignments.DTOs;
using MyCondo.Application.Features.Security.ServiceProviderAssignments.Mappings;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Property.Flats;
using MyCondo.Domain.Features.Security.Common;
using MyCondo.Domain.Features.Security.ServiceProviderAssignments;
using MyCondo.Domain.Features.Security.ServiceProviders;

namespace MyCondo.Application.Features.Security.ServiceProviderAssignments.Commands.CreateServiceProviderAssignment;

public sealed class CreateServiceProviderAssignmentCommandHandler(
    IServiceProviderAssignmentRepository assignments,
    IServiceProviderProfileRepository profiles,
    IFlatRepository flats,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    IClock clock,
    ILogger<CreateServiceProviderAssignmentCommandHandler> logger
) : IRequestHandler<CreateServiceProviderAssignmentCommand, ServiceProviderAssignmentDto>
{
    public async ValueTask<ServiceProviderAssignmentDto> Handle(CreateServiceProviderAssignmentCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        ServiceProviderProfileId providerId = new(command.ServiceProviderProfileId);
        ServiceProviderProfile provider = await profiles.GetByIdAsync(providerId, cancellationToken)
            ?? throw new NotFoundException(nameof(ServiceProviderProfile), command.ServiceProviderProfileId);
        if (provider.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(ServiceProviderProfile), command.ServiceProviderProfileId);
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

        ServiceProviderAssignment assignment = ServiceProviderAssignment.Create(
            tenantId, providerId, flatId, command.ValidFromUtc, command.ValidToUtc, allowedDays,
            command.AllowedStartTime, command.AllowedEndTime, clock.UtcNow);

        assignments.Add(assignment);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Service provider assignment {AssignmentId} created for provider {ProviderId}, flat {FlatId}, tenant {TenantId}",
            assignment.Id, providerId, flatId, tenantId);

        return assignment.ToDto();
    }
}
