using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Auth.DTOs;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Attachments;
using MyCondo.Domain.Features.Identity.Users;

namespace MyCondo.Application.Features.Auth.Commands.UploadMyAvatar;

public sealed class UploadMyAvatarCommandHandler(
    IUserRepository users,
    IAttachmentRepository attachments,
    IFileStorageService fileStorage,
    IUnitOfWork unitOfWork,
    IUserContextResolver userContextResolver,
    ICurrentUserProvider currentUser,
    IClock clock,
    ILogger<UploadMyAvatarCommandHandler> logger
) : IRequestHandler<UploadMyAvatarCommand, UserProfileDto>
{
    public async ValueTask<UserProfileDto> Handle(UploadMyAvatarCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not Guid userIdValue || currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        UserId userId = new(userIdValue);
        User user = await users.GetByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException(nameof(User), userIdValue);

        Attachment? previousAvatar = user.AvatarAttachmentId is Guid previousAttachmentId
            ? await attachments.GetByIdAsync(new AttachmentId(previousAttachmentId), cancellationToken)
            : null;

        string storageKey = await fileStorage.SaveAsync(
            command.Content, command.FileName, command.ContentType, cancellationToken);

        DateTimeOffset now = clock.UtcNow;

        Attachment newAvatar = Attachment.Record(
            tenantId, AttachmentOwnerType.User, userIdValue, storageKey, command.FileName,
            command.ContentType, command.SizeBytes, now);
        attachments.Add(newAvatar);

        user.SetAvatar(newAvatar.Id.Value, now);

        if (previousAvatar is not null)
        {
            previousAvatar.Delete(now, userIdValue);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        if (previousAvatar is not null)
        {
            await fileStorage.DeleteAsync(previousAvatar.StorageKey, cancellationToken);
        }

        logger.LogInformation("Avatar updated for user {UserId}", userId);

        return await userContextResolver.ResolveProfileAsync(user, cancellationToken);
    }
}
