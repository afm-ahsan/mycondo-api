using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Auth.Commands.RemoveMyAvatar;
using MyCondo.Application.Features.Auth.DTOs;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Attachments;
using MyCondo.Domain.Features.Identity.Users;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Auth.Commands.RemoveMyAvatar;

public class RemoveMyAvatarCommandHandlerTests
{
    private static readonly DateTimeOffset NowUtc = new(2026, 8, 10, 0, 0, 0, TimeSpan.Zero);

    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IAttachmentRepository _attachments = Substitute.For<IAttachmentRepository>();
    private readonly IFileStorageService _fileStorage = Substitute.For<IFileStorageService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IUserContextResolver _userContextResolver = Substitute.For<IUserContextResolver>();
    private readonly ICurrentUserProvider _currentUser = Substitute.For<ICurrentUserProvider>();
    private readonly IClock _clock = Substitute.For<IClock>();

    public RemoveMyAvatarCommandHandlerTests()
    {
        _clock.UtcNow.Returns(NowUtc);
    }

    private RemoveMyAvatarCommandHandler CreateHandler() => new(
        _users, _attachments, _fileStorage, _unitOfWork, _userContextResolver, _currentUser, _clock,
        Substitute.For<ILogger<RemoveMyAvatarCommandHandler>>());

    [Fact]
    public async Task Removes_Avatar_And_Deletes_The_File_When_One_Is_Set()
    {
        Guid tenantId = Guid.NewGuid();
        User user = User.Register(tenantId, "jane@example.com", "hash", "Jane Doe", null, NowUtc);
        Attachment avatar = Attachment.Record(
            tenantId, AttachmentOwnerType.User, user.Id.Value, "storage-key.png", "photo.png", "image/png", 1024, NowUtc);
        user.SetAvatar(avatar.Id.Value, NowUtc);

        _currentUser.UserId.Returns(user.Id.Value);
        _users.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _attachments.GetByIdAsync(avatar.Id, Arg.Any<CancellationToken>()).Returns(avatar);
        _userContextResolver.ResolveProfileAsync(user, Arg.Any<CancellationToken>())
            .Returns(new UserProfileDto(user.Id.Value, tenantId, user.Email, user.FullName, null, NowUtc, null, [], []));

        await CreateHandler().Handle(new RemoveMyAvatarCommand(), CancellationToken.None);

        user.AvatarAttachmentId.Should().BeNull();
        avatar.DeletedAtUtc.Should().NotBeNull();
        await _fileStorage.Received(1).DeleteAsync("storage-key.png", Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Is_A_NoOp_When_User_Has_No_Avatar()
    {
        Guid tenantId = Guid.NewGuid();
        User user = User.Register(tenantId, "jane@example.com", "hash", "Jane Doe", null, NowUtc);

        _currentUser.UserId.Returns(user.Id.Value);
        _users.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        _userContextResolver.ResolveProfileAsync(user, Arg.Any<CancellationToken>())
            .Returns(new UserProfileDto(user.Id.Value, tenantId, user.Email, user.FullName, null, NowUtc, null, [], []));

        await CreateHandler().Handle(new RemoveMyAvatarCommand(), CancellationToken.None);

        await _fileStorage.DidNotReceive().DeleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
