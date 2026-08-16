using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Attachments.Commands.DeleteAttachment;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Attachments;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Attachments.Commands.DeleteAttachment;

public class DeleteAttachmentCommandHandlerTests
{
    private static readonly DateTimeOffset NowUtc = new(2026, 8, 16, 0, 0, 0, TimeSpan.Zero);

    private readonly IAttachmentRepository _attachments = Substitute.For<IAttachmentRepository>();
    private readonly IFileStorageService _fileStorage = Substitute.For<IFileStorageService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUserProvider _currentUser = Substitute.For<ICurrentUserProvider>();
    private readonly IClock _clock = Substitute.For<IClock>();

    public DeleteAttachmentCommandHandlerTests()
    {
        _clock.UtcNow.Returns(NowUtc);
    }

    private DeleteAttachmentCommandHandler CreateHandler() => new(
        _attachments, _fileStorage, _unitOfWork, _currentUser, _clock,
        Substitute.For<ILogger<DeleteAttachmentCommandHandler>>());

    [Fact]
    public async Task Soft_Deletes_And_Deletes_The_Physical_File()
    {
        Guid tenantId = Guid.NewGuid();
        Attachment attachment = Attachment.Record(
            tenantId, AttachmentOwnerType.Resident, Guid.NewGuid(), "key.jpg", "photo.jpg", "image/jpeg", 10, NowUtc);
        _currentUser.TenantId.Returns(tenantId);
        _attachments.GetByIdAsync(attachment.Id, Arg.Any<CancellationToken>()).Returns(attachment);

        await CreateHandler().Handle(new DeleteAttachmentCommand(attachment.Id.Value), CancellationToken.None);

        attachment.DeletedAtUtc.Should().NotBeNull();
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _fileStorage.Received(1).DeleteAsync("key.jpg", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Throws_NotFound_For_Cross_Tenant_Attachment()
    {
        Attachment attachment = Attachment.Record(
            Guid.NewGuid(), AttachmentOwnerType.Resident, Guid.NewGuid(), "key.jpg", "photo.jpg", "image/jpeg", 10, NowUtc);
        _currentUser.TenantId.Returns(Guid.NewGuid());
        _attachments.GetByIdAsync(attachment.Id, Arg.Any<CancellationToken>()).Returns(attachment);

        Func<Task> act = () => CreateHandler().Handle(new DeleteAttachmentCommand(attachment.Id.Value), CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<NotFoundException>();
        await _fileStorage.DidNotReceive().DeleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
