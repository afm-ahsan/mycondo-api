using Mediator;

namespace MyCondo.Application.Features.Property.Gates.Commands.DeactivateGate;

public sealed record DeactivateGateCommand(Guid GateId) : IRequest;
