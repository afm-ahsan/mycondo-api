using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Security.Guests.DTOs;

namespace MyCondo.Application.Features.Security.Guests.Queries.GetGuestProfileByPhone;

public sealed record GetGuestProfileByPhoneQuery(string Phone) : IRequest<GuestProfileDto?>, IRequiresFeature
{
    public string FeatureKey => "security.visitors";
}
