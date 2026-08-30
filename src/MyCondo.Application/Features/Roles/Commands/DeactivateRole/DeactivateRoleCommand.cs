using Mediator;
using MyCondo.Application.Common.Abstractions;

namespace MyCondo.Application.Features.Roles.Commands.DeactivateRole;

public sealed record DeactivateRoleCommand(Guid RoleId) : IRequest, ILifecycleWriteOperation;
