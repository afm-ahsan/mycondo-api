using Mediator;

namespace MyCondo.Application.Features.Security.Guests.Commands.UnblockGuestProfile;

public sealed record UnblockGuestProfileCommand(Guid GuestProfileId) : IRequest;
