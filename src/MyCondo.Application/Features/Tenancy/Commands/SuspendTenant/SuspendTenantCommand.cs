using Mediator;

namespace MyCondo.Application.Features.Tenancy.Commands.SuspendTenant;

public sealed record SuspendTenantCommand(Guid TenantId) : IRequest;
