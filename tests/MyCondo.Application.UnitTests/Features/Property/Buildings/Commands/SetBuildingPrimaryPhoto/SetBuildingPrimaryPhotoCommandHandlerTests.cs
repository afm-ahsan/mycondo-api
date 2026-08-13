using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Property.Buildings.Commands.SetBuildingPrimaryPhoto;
using MyCondo.Application.Features.Property.Buildings.DTOs;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Attachments;
using MyCondo.Domain.Features.Property.Buildings;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Property.Buildings.Commands.SetBuildingPrimaryPhoto;

public class SetBuildingPrimaryPhotoCommandHandlerTests
{
    private static readonly DateTimeOffset NowUtc = new(2026, 8, 13, 0, 0, 0, TimeSpan.Zero);
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid OtherTenantId = Guid.NewGuid();

    private readonly IBuildingRepository _buildings = Substitute.For<IBuildingRepository>();
    private readonly IAttachmentRepository _attachments = Substitute.For<IAttachmentRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUserProvider _currentUser = Substitute.For<ICurrentUserProvider>();

    public SetBuildingPrimaryPhotoCommandHandlerTests()
    {
        _currentUser.TenantId.Returns(TenantId);
    }

    private SetBuildingPrimaryPhotoCommandHandler CreateHandler() => new(
        _buildings, _attachments, _unitOfWork, _currentUser,
        Substitute.For<ILogger<SetBuildingPrimaryPhotoCommandHandler>>());

    [Fact]
    public async Task Sets_Primary_Photo_When_Attachment_Belongs_To_The_Building()
    {
        Building building = Building.Create(TenantId, "Tower A", "TA", null, NowUtc);
        _buildings.GetByIdAsync(building.Id, Arg.Any<CancellationToken>()).Returns(building);

        Attachment attachment = Attachment.Record(
            TenantId, AttachmentOwnerType.Building, building.Id.Value, "local/key", "photo.jpg", "image/jpeg", 1024, NowUtc);
        _attachments.GetByIdAsync(attachment.Id, Arg.Any<CancellationToken>()).Returns(attachment);

        BuildingDto result = await CreateHandler().Handle(
            new SetBuildingPrimaryPhotoCommand(building.Id.Value, attachment.Id.Value), CancellationToken.None);

        result.PrimaryPhotoAttachmentId.Should().Be(attachment.Id.Value);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Clears_Primary_Photo_When_AttachmentId_Is_Null()
    {
        Building building = Building.Create(TenantId, "Tower A", "TA", null, NowUtc);
        building.SetPrimaryPhoto(Guid.NewGuid());
        _buildings.GetByIdAsync(building.Id, Arg.Any<CancellationToken>()).Returns(building);

        BuildingDto result = await CreateHandler().Handle(
            new SetBuildingPrimaryPhotoCommand(building.Id.Value, null), CancellationToken.None);

        result.PrimaryPhotoAttachmentId.Should().BeNull();
    }

    [Fact]
    public async Task Throws_NotFound_When_Attachment_Belongs_To_A_Different_Tenant()
    {
        Building building = Building.Create(TenantId, "Tower A", "TA", null, NowUtc);
        _buildings.GetByIdAsync(building.Id, Arg.Any<CancellationToken>()).Returns(building);

        Attachment attachment = Attachment.Record(
            OtherTenantId, AttachmentOwnerType.Building, building.Id.Value, "local/key", "photo.jpg", "image/jpeg", 1024, NowUtc);
        _attachments.GetByIdAsync(attachment.Id, Arg.Any<CancellationToken>()).Returns(attachment);

        Func<Task> act = async () => await CreateHandler().Handle(
            new SetBuildingPrimaryPhotoCommand(building.Id.Value, attachment.Id.Value), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Throws_NotFound_When_Attachment_Belongs_To_A_Different_Building()
    {
        Building building = Building.Create(TenantId, "Tower A", "TA", null, NowUtc);
        Building other = Building.Create(TenantId, "Tower B", "TB", null, NowUtc);
        _buildings.GetByIdAsync(building.Id, Arg.Any<CancellationToken>()).Returns(building);

        Attachment attachment = Attachment.Record(
            TenantId, AttachmentOwnerType.Building, other.Id.Value, "local/key", "photo.jpg", "image/jpeg", 1024, NowUtc);
        _attachments.GetByIdAsync(attachment.Id, Arg.Any<CancellationToken>()).Returns(attachment);

        Func<Task> act = async () => await CreateHandler().Handle(
            new SetBuildingPrimaryPhotoCommand(building.Id.Value, attachment.Id.Value), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Throws_NotFound_When_Building_Belongs_To_A_Different_Tenant()
    {
        Building building = Building.Create(OtherTenantId, "Tower A", "TA", null, NowUtc);
        _buildings.GetByIdAsync(building.Id, Arg.Any<CancellationToken>()).Returns(building);

        Func<Task> act = async () => await CreateHandler().Handle(
            new SetBuildingPrimaryPhotoCommand(building.Id.Value, null), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
