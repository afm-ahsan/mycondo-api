using Mediator;

namespace MyCondo.Application.Features.Residents.Commands.DisableResident;

public sealed record DisableResidentCommand(Guid ResidentId) : IRequest;
