using Mediator;

namespace MyCondo.Application.Features.Tenancy.Commands.ProvisionTenant;

public sealed record ProvisionTenantCommand(
    string Name,
    string Slug
) : IRequest<ProvisionTenantResult>;
