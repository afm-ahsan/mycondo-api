using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Residents.HouseholdMembers.Commands.SetOwnerHouseholdMemberPrimaryPhoto;
using MyCondo.Application.Features.Residents.HouseholdMembers.DTOs;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Attachments;
using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Domain.Features.Property.Flats;
using MyCondo.Domain.Features.Residents;
using MyCondo.Domain.Features.Residents.HouseholdMembers;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Residents.HouseholdMembers.Commands.SetOwnerHouseholdMemberPrimaryPhoto;

public class SetOwnerHouseholdMemberPrimaryPhotoCommandHandlerTests
{
    private static readonly DateTimeOffset NowUtc = new(2026, 8, 16, 0, 0, 0, TimeSpan.Zero);

    private readonly IResidentHouseholdMemberRepository _members = Substitute.For<IResidentHouseholdMemberRepository>();
    private readonly IResidentRepository _residents = Substitute.For<IResidentRepository>();
    private readonly IFlatRepository _flats = Substitute.For<IFlatRepository>();
    private readonly IAttachmentRepository _attachments = Substitute.For<IAttachmentRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUserProvider _currentUser = Substitute.For<ICurrentUserProvider>();

    private SetOwnerHouseholdMemberPrimaryPhotoCommandHandler CreateHandler() => new(
        _members, _residents, _flats, _attachments, _unitOfWork, _currentUser,
        Substitute.For<ILogger<SetOwnerHouseholdMemberPrimaryPhotoCommandHandler>>());

    private (ResidentHouseholdMember Member, Guid TenantId) SetUpMemberWithPermission(bool hasPermission = true)
    {
        Guid tenantId = Guid.NewGuid();
        BuildingId buildingId = BuildingId.New();
        Flat flat = Flat.Create(tenantId, buildingId, "A-1", 1, FlatType.Residential, NowUtc);
        Resident resident = Resident.Register(tenantId, flat.Id, "Jane Doe", null, null, ResidentType.Owner, NowUtc);
        ResidentHouseholdMember member = ResidentHouseholdMember.Add(
            tenantId, resident.Id.Value, "Fatema Ahmed", RelationshipType.Spouse, "Female", new DateOnly(1992, 5, 1),
            null, null, null, null, null, null, NowUtc);

        _currentUser.TenantId.Returns(tenantId);
        _currentUser.HasPermissionForBuilding("ownership.manage", buildingId.Value).Returns(hasPermission);
        _members.GetByIdAsync(member.Id, Arg.Any<CancellationToken>()).Returns(member);
        _residents.GetByIdAsync(resident.Id, Arg.Any<CancellationToken>()).Returns(resident);
        _flats.GetByIdAsync(flat.Id, Arg.Any<CancellationToken>()).Returns(flat);

        return (member, tenantId);
    }

    private Attachment SetUpAttachment(Guid tenantId, AttachmentOwnerType ownerType, Guid ownerId)
    {
        Attachment attachment = Attachment.Record(
            tenantId, ownerType, ownerId, "key.jpg", "photo.jpg", "image/jpeg", 100, NowUtc);
        _attachments.GetByIdAsync(attachment.Id, Arg.Any<CancellationToken>()).Returns(attachment);
        return attachment;
    }

    [Fact]
    public async Task Sets_PrimaryPhoto_When_Attachment_Belongs_To_Member()
    {
        (ResidentHouseholdMember member, Guid tenantId) = SetUpMemberWithPermission();
        Attachment attachment = SetUpAttachment(tenantId, AttachmentOwnerType.ResidentHouseholdMember, member.Id.Value);
        SetOwnerHouseholdMemberPrimaryPhotoCommand command = new(member.Id.Value, attachment.Id.Value);

        ResidentHouseholdMemberDto result = await CreateHandler().Handle(command, CancellationToken.None);

        result.PrimaryPhotoAttachmentId.Should().Be(attachment.Id.Value);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Clears_PrimaryPhoto_When_AttachmentId_Is_Null()
    {
        (ResidentHouseholdMember member, _) = SetUpMemberWithPermission();
        member.SetPrimaryPhoto(Guid.NewGuid());
        SetOwnerHouseholdMemberPrimaryPhotoCommand command = new(member.Id.Value, null);

        ResidentHouseholdMemberDto result = await CreateHandler().Handle(command, CancellationToken.None);

        result.PrimaryPhotoAttachmentId.Should().BeNull();
    }

    [Fact]
    public async Task Throws_NotFound_When_Attachment_Does_Not_Exist()
    {
        (ResidentHouseholdMember member, _) = SetUpMemberWithPermission();
        _attachments.GetByIdAsync(Arg.Any<AttachmentId>(), Arg.Any<CancellationToken>()).Returns((Attachment?)null);
        SetOwnerHouseholdMemberPrimaryPhotoCommand command = new(member.Id.Value, Guid.NewGuid());

        Func<Task> act = () => CreateHandler().Handle(command, CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Throws_NotFound_When_Attachment_Belongs_To_Different_Owner()
    {
        (ResidentHouseholdMember member, Guid tenantId) = SetUpMemberWithPermission();
        Attachment attachment = SetUpAttachment(tenantId, AttachmentOwnerType.ResidentHouseholdMember, Guid.NewGuid());
        SetOwnerHouseholdMemberPrimaryPhotoCommand command = new(member.Id.Value, attachment.Id.Value);

        Func<Task> act = () => CreateHandler().Handle(command, CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Throws_NotFound_When_Attachment_Belongs_To_Different_Tenant()
    {
        (ResidentHouseholdMember member, _) = SetUpMemberWithPermission();
        Attachment attachment = SetUpAttachment(
            Guid.NewGuid(), AttachmentOwnerType.ResidentHouseholdMember, member.Id.Value);
        SetOwnerHouseholdMemberPrimaryPhotoCommand command = new(member.Id.Value, attachment.Id.Value);

        Func<Task> act = () => CreateHandler().Handle(command, CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Throws_Forbidden_When_User_Lacks_OwnershipManage_Permission_For_Building()
    {
        (ResidentHouseholdMember member, _) = SetUpMemberWithPermission(hasPermission: false);
        SetOwnerHouseholdMemberPrimaryPhotoCommand command = new(member.Id.Value, null);

        Func<Task> act = () => CreateHandler().Handle(command, CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<ForbiddenException>();
    }
}
