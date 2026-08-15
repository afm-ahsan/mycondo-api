using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Auth.Commands.UploadMyAvatar;
using MyCondo.Application.Features.Auth.DTOs;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Attachments;
using MyCondo.Domain.Features.Identity.Users;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Auth.Commands.UploadMyAvatar;

public class UploadMyAvatarCommandHandlerTests
{
    private static readonly DateTimeOffset NowUtc = new(2026, 8, 10, 0, 0, 0, TimeSpan.Zero);

    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IAttachmentRepository _attachments = Substitute.For<IAttachmentRepository>();
    private readonly IFileStorageService _fileStorage = Substitute.For<IFileStorageService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IUserContextResolver _userContextResolver = Substitute.For<IUserContextResolver>();
    private readonly ICurrentUserProvider _currentUser = Substitute.For<ICurrentUserProvider>();
    private readonly IClock _clock = Substitute.For<IClock>();

    public UploadMyAvatarCommandHandlerTests()
    {
        _clock.UtcNow.Returns(NowUtc);
    }

    private UploadMyAvatarCommandHandler CreateHandler() => new(
        _users, _attachments, _fileStorage, _unitOfWork, _userContextResolver, _currentUser, _clock,
        Substitute.For<ILogger<UploadMyAvatarCommandHandler>>());

    private (User User, Guid TenantId) SetUpAuthenticatedUser(string? existingAvatarStorageKey = null)
    {
        Guid tenantId = Guid.NewGuid();
        User user = User.Register(tenantId, "jane@example.com", "hash", "Jane Doe", null, NowUtc);

        if (existingAvatarStorageKey is not null)
        {
            Attachment existing = Attachment.Record(
                tenantId, AttachmentOwnerType.User, user.Id.Value, existingAvatarStorageKey, "old.png",
                "image/png", 1024, NowUtc);
            user.SetAvatar(existing.Id.Value, NowUtc);
            _attachments.GetByIdAsync(existing.Id, Arg.Any<CancellationToken>()).Returns(existing);
        }

        _currentUser.UserId.Returns(user.Id.Value);
        _currentUser.TenantId.Returns(tenantId);
        _users.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _userContextResolver.ResolveProfileAsync(user, Arg.Any<CancellationToken>())
            .Returns(new UserProfileDto(user.Id.Value, tenantId, user.Email, user.FullName, null, NowUtc, null, [], []));

        return (user, tenantId);
    }

    [Fact]
    public async Task Sets_Avatar_When_User_Has_None_Yet()
    {
        (User user, _) = SetUpAuthenticatedUser();
        _fileStorage.SaveAsync(Arg.Any<Stream>(), "photo.png", "image/png", Arg.Any<CancellationToken>())
            .Returns("new-storage-key.png");

        using MemoryStream content = new([1, 2, 3]);
        UploadMyAvatarCommand command = new(content, "photo.png", "image/png", 3);

        await CreateHandler().Handle(command, CancellationToken.None);

        user.AvatarAttachmentId.Should().NotBeNull();
        _attachments.Received(1).Add(Arg.Is<Attachment>(a => a.StorageKey == "new-storage-key.png"));
        await _fileStorage.DidNotReceive().DeleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Replaces_Existing_Avatar_And_Deletes_The_Old_File()
    {
        SetUpAuthenticatedUser(existingAvatarStorageKey: "old-storage-key.png");
        _fileStorage.SaveAsync(Arg.Any<Stream>(), "photo.png", "image/png", Arg.Any<CancellationToken>())
            .Returns("new-storage-key.png");

        using MemoryStream content = new([1, 2, 3]);
        UploadMyAvatarCommand command = new(content, "photo.png", "image/png", 3);

        await CreateHandler().Handle(command, CancellationToken.None);

        await _fileStorage.Received(1).DeleteAsync("old-storage-key.png", Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
