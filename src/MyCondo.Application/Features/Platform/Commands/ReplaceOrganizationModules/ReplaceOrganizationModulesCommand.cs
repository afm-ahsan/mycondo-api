using Mediator;

namespace MyCondo.Application.Features.Platform.Commands.ReplaceOrganizationModules;

public sealed record ReplaceOrganizationModulesCommand(
    Guid OrganizationId,
    IReadOnlyList<string> ModuleKeys
) : IRequest;
