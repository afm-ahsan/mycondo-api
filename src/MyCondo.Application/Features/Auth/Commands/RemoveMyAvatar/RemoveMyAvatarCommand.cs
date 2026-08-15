using Mediator;
using MyCondo.Application.Features.Auth.DTOs;

namespace MyCondo.Application.Features.Auth.Commands.RemoveMyAvatar;

public sealed record RemoveMyAvatarCommand : IRequest<UserProfileDto>;
