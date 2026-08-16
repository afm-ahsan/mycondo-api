using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Leasing.Commands.SetHouseholdMemberPrimaryPhoto;
using MyCondo.Application.Features.Leasing.DTOs;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Attachments;
using MyCondo.Domain.Features.Leasing.HouseholdMembers;
using MyCondo.Domain.Features.Leasing.OccupancyRegistrations;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Leasing.Commands.SetHouseholdMemberPrimaryPhoto;

public class SetHouseholdMemberPrimaryPhotoCommandHandlerTests
{
    private static readonly DateTimeOffset NowUtc = new(2026, 8, 16, 0, 0, 0, TimeSpan.Zero);

    private readonly IHouseholdMemberRepository _members = Substitute.For<IHouseholdMemberRepository>();
    private readonly IAttachmentRepository _attachments = Substitute.For<IAttachmentRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUserProvider _currentUser = Substitute.For<ICurrentUserProvider>();

    private SetHouseholdMemberPrimaryPhotoCommandHandler CreateHandler() => new(
        _members, _attachments, _unitOfWork, _currentUser,
        Substitute.For<ILogger<SetHouseholdMemberPrimaryPhotoCommandHandler>>());

    private (HouseholdMember Member, Guid TenantId) SetUpMember()
    {
        Guid tenantId = Guid.NewGuid();
        HouseholdMember member = HouseholdMember.Add(
            tenantId, OccupancyRegistrationId.New(), "John Doe", "Spouse", null, null, null, "Male", null, null,
            null, null, null, NowUtc);

        _currentUser.TenantId.Returns(tenantId);
        _members.GetByIdAsync(member.Id, Arg.Any<CancellationToken>()).Returns(member);

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
        (HouseholdMember member, Guid tenantId) = SetUpMember();
        Attachment attachment = SetUpAttachment(tenantId, AttachmentOwnerType.LeasingHouseholdMember, member.Id.Value);
        SetHouseholdMemberPrimaryPhotoCommand command = new(member.Id.Value, attachment.Id.Value);

        HouseholdMemberDto result = await CreateHandler().Handle(command, CancellationToken.None);

        result.PrimaryPhotoAttachmentId.Should().Be(attachment.Id.Value);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Clears_PrimaryPhoto_When_AttachmentId_Is_Null()
    {
        (HouseholdMember member, _) = SetUpMember();
        member.SetPrimaryPhoto(Guid.NewGuid());
        SetHouseholdMemberPrimaryPhotoCommand command = new(member.Id.Value, null);

        HouseholdMemberDto result = await CreateHandler().Handle(command, CancellationToken.None);

        result.PrimaryPhotoAttachmentId.Should().BeNull();
    }

    [Fact]
    public async Task Throws_NotFound_When_Attachment_Does_Not_Exist()
    {
        (HouseholdMember member, _) = SetUpMember();
        _attachments.GetByIdAsync(Arg.Any<AttachmentId>(), Arg.Any<CancellationToken>()).Returns((Attachment?)null);
        SetHouseholdMemberPrimaryPhotoCommand command = new(member.Id.Value, Guid.NewGuid());

        Func<Task> act = () => CreateHandler().Handle(command, CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Throws_NotFound_When_Attachment_Belongs_To_Different_Owner()
    {
        (HouseholdMember member, Guid tenantId) = SetUpMember();
        Attachment attachment = SetUpAttachment(tenantId, AttachmentOwnerType.LeasingHouseholdMember, Guid.NewGuid());
        SetHouseholdMemberPrimaryPhotoCommand command = new(member.Id.Value, attachment.Id.Value);

        Func<Task> act = () => CreateHandler().Handle(command, CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Throws_NotFound_When_Member_Belongs_To_Different_Tenant()
    {
        (HouseholdMember member, _) = SetUpMember();
        _currentUser.TenantId.Returns(Guid.NewGuid());
        SetHouseholdMemberPrimaryPhotoCommand command = new(member.Id.Value, null);

        Func<Task> act = () => CreateHandler().Handle(command, CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
