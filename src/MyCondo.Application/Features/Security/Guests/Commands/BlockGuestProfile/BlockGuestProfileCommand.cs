using Mediator;

namespace MyCondo.Application.Features.Security.Guests.Commands.BlockGuestProfile;

public sealed record BlockGuestProfileCommand(Guid GuestProfileId, string Reason) : IRequest;
