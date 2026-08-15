using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Domain.Features.Attachments;
using MyCondo.Domain.Features.Identity.Users;

namespace MyCondo.Application.Features.Auth.Queries.GetMyAvatar;

public sealed class GetMyAvatarQueryHandler(
    IUserRepository users,
    IAttachmentRepository attachments,
    IFileStorageService fileStorage,
    ICurrentUserProvider currentUser
) : IRequestHandler<GetMyAvatarQuery, AvatarContentDto?>
{
    public async ValueTask<AvatarContentDto?> Handle(GetMyAvatarQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not Guid userIdValue)
        {
            throw new ForbiddenException("Authentication required.");
        }

        UserId userId = new(userIdValue);
        User user = await users.GetByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException(nameof(User), userIdValue);

        if (user.AvatarAttachmentId is not Guid attachmentId)
        {
            return null;
        }

        Attachment? avatar = await attachments.GetByIdAsync(new AttachmentId(attachmentId), cancellationToken);
        if (avatar is null)
        {
            return null;
        }

        Stream? content = await fileStorage.OpenReadAsync(avatar.StorageKey, cancellationToken);
        return content is null ? null : new AvatarContentDto(content, avatar.ContentType);
    }
}
