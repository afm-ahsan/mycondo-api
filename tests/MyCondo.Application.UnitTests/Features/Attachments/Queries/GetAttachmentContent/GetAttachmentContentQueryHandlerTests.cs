using AwesomeAssertions;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Attachments.Queries.GetAttachmentContent;
using MyCondo.Domain.Features.Attachments;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Attachments.Queries.GetAttachmentContent;

public class GetAttachmentContentQueryHandlerTests
{
    private static readonly DateTimeOffset NowUtc = new(2026, 8, 16, 0, 0, 0, TimeSpan.Zero);

    private readonly IAttachmentRepository _attachments = Substitute.For<IAttachmentRepository>();
    private readonly IFileStorageService _fileStorage = Substitute.For<IFileStorageService>();
    private readonly ICurrentUserProvider _currentUser = Substitute.For<ICurrentUserProvider>();

    private GetAttachmentContentQueryHandler CreateHandler() => new(_attachments, _fileStorage, _currentUser);

    [Fact]
    public async Task Returns_Content_For_Own_Tenant_Attachment()
    {
        Guid tenantId = Guid.NewGuid();
        Attachment attachment = Attachment.Record(
            tenantId, AttachmentOwnerType.Resident, Guid.NewGuid(), "key.jpg", "photo.jpg", "image/jpeg", 10, NowUtc);
        _currentUser.TenantId.Returns(tenantId);
        _attachments.GetByIdAsync(attachment.Id, Arg.Any<CancellationToken>()).Returns(attachment);
        using MemoryStream stream = new([1, 2, 3]);
        _fileStorage.OpenReadAsync("key.jpg", Arg.Any<CancellationToken>()).Returns(stream);

        AttachmentContentDto? result = await CreateHandler().Handle(
            new GetAttachmentContentQuery(attachment.Id.Value), CancellationToken.None);

        result.Should().NotBeNull();
        result!.ContentType.Should().Be("image/jpeg");
        result.FileName.Should().Be("photo.jpg");
    }

    [Fact]
    public async Task Returns_Null_For_Cross_Tenant_Attachment()
    {
        Attachment attachment = Attachment.Record(
            Guid.NewGuid(), AttachmentOwnerType.Resident, Guid.NewGuid(), "key.jpg", "photo.jpg", "image/jpeg", 10, NowUtc);
        _currentUser.TenantId.Returns(Guid.NewGuid());
        _attachments.GetByIdAsync(attachment.Id, Arg.Any<CancellationToken>()).Returns(attachment);

        AttachmentContentDto? result = await CreateHandler().Handle(
            new GetAttachmentContentQuery(attachment.Id.Value), CancellationToken.None);

        result.Should().BeNull();
        await _fileStorage.DidNotReceive().OpenReadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Returns_Null_When_Attachment_Not_Found()
    {
        Guid tenantId = Guid.NewGuid();
        _currentUser.TenantId.Returns(tenantId);
        _attachments.GetByIdAsync(Arg.Any<AttachmentId>(), Arg.Any<CancellationToken>()).Returns((Attachment?)null);

        AttachmentContentDto? result = await CreateHandler().Handle(
            new GetAttachmentContentQuery(Guid.NewGuid()), CancellationToken.None);

        result.Should().BeNull();
    }
}
