using Mediator;

namespace MyCondo.Application.Features.Auth.Queries.GetMyAvatar;

public sealed record GetMyAvatarQuery : IRequest<AvatarContentDto?>;

public sealed record AvatarContentDto(Stream Content, string ContentType);
