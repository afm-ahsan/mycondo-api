using Mediator;

namespace MyCondo.Application.Features.Tenancy.Commands.ActivateTenant;

public sealed record ActivateTenantCommand(Guid TenantId) : IRequest;
