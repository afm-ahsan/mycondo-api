using Mediator;

namespace MyCondo.Application.Features.Property.Gates.Commands.ActivateGate;

public sealed record ActivateGateCommand(Guid GateId) : IRequest;
