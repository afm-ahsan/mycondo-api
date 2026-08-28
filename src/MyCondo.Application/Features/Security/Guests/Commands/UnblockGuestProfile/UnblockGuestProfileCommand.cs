using Mediator;
using MyCondo.Application.Common.Abstractions;

namespace MyCondo.Application.Features.Security.Guests.Commands.UnblockGuestProfile;

public sealed record UnblockGuestProfileCommand(Guid GuestProfileId) : IRequest, IRequiresFeature
{
    public string FeatureKey => "security.visitors";
}
