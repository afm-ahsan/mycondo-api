using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Tenancy;

namespace MyCondo.Application.Features.Tenancy.Commands.ProvisionTenant;

public sealed class ProvisionTenantCommandHandler(
    ITenantRepository tenants,
    IUnitOfWork unitOfWork,
    IClock clock,
    ILogger<ProvisionTenantCommandHandler> logger
) : IRequestHandler<ProvisionTenantCommand, ProvisionTenantResult>
{
    public async ValueTask<ProvisionTenantResult> Handle(ProvisionTenantCommand command, CancellationToken cancellationToken)
    {
        string normalizedSlug = command.Slug.Trim().ToLowerInvariant();

        bool slugTaken = await tenants.SlugExistsAsync(normalizedSlug, cancellationToken);
        if (slugTaken)
        {
            throw new ConflictException($"A tenant with slug '{normalizedSlug}' already exists.");
        }

        Tenant tenant = Tenant.Provision(command.Name, normalizedSlug, clock.UtcNow);

        tenants.Add(tenant);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Tenant {TenantId} provisioned with slug {Slug}", tenant.Id, tenant.Slug);

        return new ProvisionTenantResult(
            TenantId: tenant.Id.Value,
            Name: tenant.Name,
            Slug: tenant.Slug,
            Status: tenant.Status.ToString());
    }
}
