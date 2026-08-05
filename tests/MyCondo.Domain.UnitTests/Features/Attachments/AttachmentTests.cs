using AwesomeAssertions;
using MyCondo.Domain.Features.Attachments;

namespace MyCondo.Domain.UnitTests.Features.Attachments;

public class AttachmentTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid OwnerId = Guid.NewGuid();

    [Fact]
    public void Record_Trims_Fields()
    {
        Attachment attachment = Attachment.Record(
            TenantId, AttachmentOwnerType.Resident, OwnerId, "  s3://key  ", "  id-card.jpg  ", "  image/jpeg  ",
            1024, Now);

        attachment.StorageKey.Should().Be("s3://key");
        attachment.FileName.Should().Be("id-card.jpg");
        attachment.ContentType.Should().Be("image/jpeg");
        attachment.SizeBytes.Should().Be(1024);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Record_Throws_When_StorageKey_Is_Blank(string storageKey)
    {
        Action act = () => Attachment.Record(
            TenantId, AttachmentOwnerType.Resident, OwnerId, storageKey, "id-card.jpg", "image/jpeg", 1024, Now);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Record_Throws_When_OwnerId_Is_Empty()
    {
        Action act = () => Attachment.Record(
            TenantId, AttachmentOwnerType.Resident, Guid.Empty, "s3://key", "id-card.jpg", "image/jpeg", 1024, Now);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Record_Throws_When_SizeBytes_Is_Not_Positive()
    {
        Action act = () => Attachment.Record(
            TenantId, AttachmentOwnerType.Resident, OwnerId, "s3://key", "id-card.jpg", "image/jpeg", 0, Now);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Delete_Sets_DeletedAtUtc()
    {
        Attachment attachment = Attachment.Record(
            TenantId, AttachmentOwnerType.Resident, OwnerId, "s3://key", "id-card.jpg", "image/jpeg", 1024, Now);

        attachment.Delete(Now.AddDays(1), Guid.NewGuid());

        attachment.DeletedAtUtc.Should().Be(Now.AddDays(1));
    }
}
