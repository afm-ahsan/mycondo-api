using Mediator;

namespace MyCondo.Application.Features.Property.Flats.Commands.DeactivateFlat;

public sealed record DeactivateFlatCommand(Guid FlatId) : IRequest;
