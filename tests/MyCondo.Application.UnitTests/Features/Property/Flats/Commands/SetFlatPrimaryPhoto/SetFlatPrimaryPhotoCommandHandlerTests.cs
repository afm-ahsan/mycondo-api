using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Property.Flats.Commands.SetFlatPrimaryPhoto;
using MyCondo.Application.Features.Property.Flats.DTOs;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Attachments;
using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Domain.Features.Property.Flats;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Property.Flats.Commands.SetFlatPrimaryPhoto;

public class SetFlatPrimaryPhotoCommandHandlerTests
{
    private static readonly DateTimeOffset NowUtc = new(2026, 8, 13, 0, 0, 0, TimeSpan.Zero);
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid OtherTenantId = Guid.NewGuid();
    private static readonly BuildingId BuildingId = new(Guid.NewGuid());

    private readonly IFlatRepository _flats = Substitute.For<IFlatRepository>();
    private readonly IAttachmentRepository _attachments = Substitute.For<IAttachmentRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUserProvider _currentUser = Substitute.For<ICurrentUserProvider>();

    public SetFlatPrimaryPhotoCommandHandlerTests()
    {
        _currentUser.TenantId.Returns(TenantId);
    }

    private SetFlatPrimaryPhotoCommandHandler CreateHandler() => new(
        _flats, _attachments, _unitOfWork, _currentUser,
        Substitute.For<ILogger<SetFlatPrimaryPhotoCommandHandler>>());

    [Fact]
    public async Task Sets_Primary_Photo_When_Attachment_Belongs_To_The_Flat()
    {
        Flat flat = Flat.Create(TenantId, BuildingId, "A-101", 1, FlatType.Residential, NowUtc);
        _flats.GetByIdAsync(flat.Id, Arg.Any<CancellationToken>()).Returns(flat);

        Attachment attachment = Attachment.Record(
            TenantId, AttachmentOwnerType.Flat, flat.Id.Value, "local/key", "photo.jpg", "image/jpeg", 1024, NowUtc);
        _attachments.GetByIdAsync(attachment.Id, Arg.Any<CancellationToken>()).Returns(attachment);

        FlatDto result = await CreateHandler().Handle(
            new SetFlatPrimaryPhotoCommand(flat.Id.Value, attachment.Id.Value), CancellationToken.None);

        result.PrimaryPhotoAttachmentId.Should().Be(attachment.Id.Value);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Throws_NotFound_When_Attachment_Belongs_To_A_Different_Tenant()
    {
        Flat flat = Flat.Create(TenantId, BuildingId, "A-101", 1, FlatType.Residential, NowUtc);
        _flats.GetByIdAsync(flat.Id, Arg.Any<CancellationToken>()).Returns(flat);

        Attachment attachment = Attachment.Record(
            OtherTenantId, AttachmentOwnerType.Flat, flat.Id.Value, "local/key", "photo.jpg", "image/jpeg", 1024, NowUtc);
        _attachments.GetByIdAsync(attachment.Id, Arg.Any<CancellationToken>()).Returns(attachment);

        Func<Task> act = async () => await CreateHandler().Handle(
            new SetFlatPrimaryPhotoCommand(flat.Id.Value, attachment.Id.Value), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Throws_NotFound_When_Attachment_Belongs_To_A_Different_Flat()
    {
        Flat flat = Flat.Create(TenantId, BuildingId, "A-101", 1, FlatType.Residential, NowUtc);
        Flat other = Flat.Create(TenantId, BuildingId, "A-102", 1, FlatType.Residential, NowUtc);
        _flats.GetByIdAsync(flat.Id, Arg.Any<CancellationToken>()).Returns(flat);

        Attachment attachment = Attachment.Record(
            TenantId, AttachmentOwnerType.Flat, other.Id.Value, "local/key", "photo.jpg", "image/jpeg", 1024, NowUtc);
        _attachments.GetByIdAsync(attachment.Id, Arg.Any<CancellationToken>()).Returns(attachment);

        Func<Task> act = async () => await CreateHandler().Handle(
            new SetFlatPrimaryPhotoCommand(flat.Id.Value, attachment.Id.Value), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Throws_NotFound_When_Flat_Belongs_To_A_Different_Tenant()
    {
        Flat flat = Flat.Create(OtherTenantId, BuildingId, "A-101", 1, FlatType.Residential, NowUtc);
        _flats.GetByIdAsync(flat.Id, Arg.Any<CancellationToken>()).Returns(flat);

        Func<Task> act = async () => await CreateHandler().Handle(
            new SetFlatPrimaryPhotoCommand(flat.Id.Value, null), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
