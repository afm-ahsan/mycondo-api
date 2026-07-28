using Mediator;
using MyCondo.Application.Features.Auth.DTOs;

namespace MyCondo.Application.Features.Auth.Queries.GetMyProfile;

public sealed record GetMyProfileQuery : IRequest<UserProfileDto>;
