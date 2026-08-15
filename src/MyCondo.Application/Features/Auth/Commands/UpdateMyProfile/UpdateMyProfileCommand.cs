using Mediator;
using MyCondo.Application.Features.Auth.DTOs;

namespace MyCondo.Application.Features.Auth.Commands.UpdateMyProfile;

public sealed record UpdateMyProfileCommand(
    string FullName,
    string? PhoneNumber
) : IRequest<UserProfileDto>;
