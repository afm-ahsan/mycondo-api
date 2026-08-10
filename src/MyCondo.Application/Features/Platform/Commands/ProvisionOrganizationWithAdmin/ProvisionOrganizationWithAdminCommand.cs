using Mediator;

namespace MyCondo.Application.Features.Platform.Commands.ProvisionOrganizationWithAdmin;

public sealed record ProvisionOrganizationWithAdminCommand(
    string Name,
    string Code,
    string Slug,
    string AdministratorFullName,
    string AdministratorEmail,
    string AdministratorPassword,
    IReadOnlyList<string> EnabledModuleKeys
) : IRequest<ProvisionOrganizationResult>;
