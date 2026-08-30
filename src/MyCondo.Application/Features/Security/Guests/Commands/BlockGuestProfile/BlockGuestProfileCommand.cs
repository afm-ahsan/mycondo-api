using Mediator;
using MyCondo.Application.Common.Abstractions;

namespace MyCondo.Application.Features.Security.Guests.Commands.BlockGuestProfile;

public sealed record BlockGuestProfileCommand(Guid GuestProfileId, string Reason) : IRequest, IRequiresFeature, ILifecycleWriteOperation
{
    public string FeatureKey => "security.visitors";
}
