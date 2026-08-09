using Mediator;

namespace MyCondo.Application.Features.Residents.Commands.LinkResidentToUser;

public sealed record LinkResidentToUserCommand(Guid ResidentId, Guid UserId) : IRequest;
