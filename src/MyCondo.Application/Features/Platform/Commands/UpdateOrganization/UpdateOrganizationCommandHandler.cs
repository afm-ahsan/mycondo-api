using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Tenancy;

namespace MyCondo.Application.Features.Platform.Commands.UpdateOrganization;

public sealed class UpdateOrganizationCommandHandler(
    ITenantRepository tenants,
    IUnitOfWork unitOfWork,
    IClock clock,
    ILogger<UpdateOrganizationCommandHandler> logger
) : IRequestHandler<UpdateOrganizationCommand>
{
    public async ValueTask<Unit> Handle(UpdateOrganizationCommand command, CancellationToken cancellationToken)
    {
        Tenant tenant = await tenants.GetByIdAsync(command.OrganizationId, cancellationToken)
            ?? throw new NotFoundException(nameof(Tenant), command.OrganizationId);

        string? normalizedCode = string.IsNullOrWhiteSpace(command.Code) ? null : command.Code.Trim().ToUpperInvariant();

        if (normalizedCode is not null
            && !string.Equals(normalizedCode, tenant.Code, StringComparison.Ordinal)
            && await tenants.CodeExistsAsync(normalizedCode, cancellationToken))
        {
            throw new ConflictException($"An organization with code '{normalizedCode}' already exists.");
        }

        tenant.UpdateDetails(command.Name, normalizedCode, clock.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Organization {TenantId} details updated", tenant.Id);
        return Unit.Value;
    }
}
