using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Auth.DTOs;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Attachments;
using MyCondo.Domain.Features.Identity.Users;

namespace MyCondo.Application.Features.Auth.Commands.RemoveMyAvatar;

public sealed class RemoveMyAvatarCommandHandler(
    IUserRepository users,
    IAttachmentRepository attachments,
    IFileStorageService fileStorage,
    IUnitOfWork unitOfWork,
    IUserContextResolver userContextResolver,
    ICurrentUserProvider currentUser,
    IClock clock,
    ILogger<RemoveMyAvatarCommandHandler> logger
) : IRequestHandler<RemoveMyAvatarCommand, UserProfileDto>
{
    public async ValueTask<UserProfileDto> Handle(RemoveMyAvatarCommand command, CancellationToken cancellationToken)
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
            // Already has no avatar — idempotent no-op, matches User.RemoveAvatar's own early return.
            return await userContextResolver.ResolveProfileAsync(user, cancellationToken);
        }

        Attachment? avatar = await attachments.GetByIdAsync(new AttachmentId(attachmentId), cancellationToken);
        DateTimeOffset now = clock.UtcNow;

        user.RemoveAvatar(now);
        avatar?.Delete(now, userIdValue);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        if (avatar is not null)
        {
            await fileStorage.DeleteAsync(avatar.StorageKey, cancellationToken);
        }

        logger.LogInformation("Avatar removed for user {UserId}", userId);

        return await userContextResolver.ResolveProfileAsync(user, cancellationToken);
    }
}
