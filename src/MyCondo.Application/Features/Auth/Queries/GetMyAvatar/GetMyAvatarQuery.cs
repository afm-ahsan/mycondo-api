using Mediator;
using MyCondo.Application.Common.Abstractions;

namespace MyCondo.Application.Features.Auth.Queries.GetMyAvatar;

public sealed record GetMyAvatarQuery : IRequest<AvatarContentDto?>, ILifecycleReadOperation;

public sealed record AvatarContentDto(Stream Content, string ContentType);
