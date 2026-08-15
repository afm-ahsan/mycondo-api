using Mediator;
using MyCondo.Application.Features.Auth.DTOs;

namespace MyCondo.Application.Features.Auth.Commands.UploadMyAvatar;

public sealed record UploadMyAvatarCommand(
    Stream Content,
    string FileName,
    string ContentType,
    long SizeBytes
) : IRequest<UserProfileDto>;
