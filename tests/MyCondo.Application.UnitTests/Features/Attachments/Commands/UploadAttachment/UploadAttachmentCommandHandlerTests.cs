using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Attachments.Commands.UploadAttachment;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Attachments;
using MyCondo.Domain.Features.Leasing.HouseholdMembers;
using MyCondo.Domain.Features.Leasing.OccupancyRegistrations;
using MyCondo.Domain.Features.Property.Flats;
using MyCondo.Domain.Features.Residents;
using MyCondo.Domain.Features.Residents.HouseholdMembers;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Attachments.Commands.UploadAttachment;

public class UploadAttachmentCommandHandlerTests
{
    private static readonly DateTimeOffset NowUtc = new(2026, 8, 16, 0, 0, 0, TimeSpan.Zero);

    private readonly IAttachmentRepository _attachments = Substitute.For<IAttachmentRepository>();
    private readonly IResidentRepository _residents = Substitute.For<IResidentRepository>();
    private readonly IResidentHouseholdMemberRepository _residentHouseholdMembers =
        Substitute.For<IResidentHouseholdMemberRepository>();
    private readonly IHouseholdMemberRepository _leasingHouseholdMembers =
        Substitute.For<IHouseholdMemberRepository>();
    private readonly IFileStorageService _fileStorage = Substitute.For<IFileStorageService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUserProvider _currentUser = Substitute.For<ICurrentUserProvider>();
    private readonly IClock _clock = Substitute.For<IClock>();

    public UploadAttachmentCommandHandlerTests()
    {
        _clock.UtcNow.Returns(NowUtc);
    }

    private UploadAttachmentCommandHandler CreateHandler() => new(
        _attachments, _residents,
        Substitute.For<Domain.Features.Leasing.OccupancyRegistrations.IOccupancyRegistrationRepository>(),
        Substitute.For<Domain.Features.Property.Buildings.IBuildingRepository>(),
        Substitute.For<Domain.Features.Property.Flats.IFlatRepository>(),
        _residentHouseholdMembers, _leasingHouseholdMembers,
        Substitute.For<Domain.Features.Expenses.Expenses.IExpenseRepository>(),
        Substitute.For<Domain.Features.Finance.FixedDeposits.IFixedDepositRepository>(),
        _fileStorage, _unitOfWork, _currentUser, _clock,
        Substitute.For<ILogger<UploadAttachmentCommandHandler>>());

    private (Resident Resident, Guid TenantId) SetUpResidentOwner()
    {
        Guid tenantId = Guid.NewGuid();
        Resident resident = Resident.Register(
            tenantId, new FlatId(Guid.NewGuid()), "Jane Doe", null, null, ResidentType.Owner, NowUtc);

        _currentUser.TenantId.Returns(tenantId);
        _residents.GetByIdAsync(resident.Id, Arg.Any<CancellationToken>()).Returns(resident);

        return (resident, tenantId);
    }

    private (ResidentHouseholdMember Member, Guid TenantId) SetUpResidentHouseholdMemberOwner()
    {
        Guid tenantId = Guid.NewGuid();
        ResidentHouseholdMember member = ResidentHouseholdMember.Add(
            tenantId, Guid.NewGuid(), "Fatema Ahmed", RelationshipType.Spouse, "Female", new DateOnly(1992, 5, 1),
            null, null, null, null, null, null, NowUtc);

        _currentUser.TenantId.Returns(tenantId);
        _residentHouseholdMembers.GetByIdAsync(member.Id, Arg.Any<CancellationToken>()).Returns(member);

        return (member, tenantId);
    }

    private (HouseholdMember Member, Guid TenantId) SetUpLeasingHouseholdMemberOwner()
    {
        Guid tenantId = Guid.NewGuid();
        HouseholdMember member = HouseholdMember.Add(
            tenantId, OccupancyRegistrationId.New(), "John Doe", "Spouse", null, null, null, "Male", null, null,
            null, null, null, NowUtc);

        _currentUser.TenantId.Returns(tenantId);
        _leasingHouseholdMembers.GetByIdAsync(member.Id, Arg.Any<CancellationToken>()).Returns(member);

        return (member, tenantId);
    }

    [Fact]
    public async Task Saves_Bytes_And_Records_Attachment_With_Server_Generated_StorageKey()
    {
        (Resident resident, _) = SetUpResidentOwner();
        _fileStorage.SaveAsync(Arg.Any<Stream>(), "deed.pdf", "application/pdf", Arg.Any<CancellationToken>())
            .Returns("server-generated-key.pdf");

        using MemoryStream content = new([1, 2, 3]);
        UploadAttachmentCommand command = new(
            content, nameof(AttachmentOwnerType.Resident), resident.Id.Value, "deed.pdf", "application/pdf", 3);

        await CreateHandler().Handle(command, CancellationToken.None);

        _attachments.Received(1).Add(Arg.Is<Attachment>(a =>
            a.StorageKey == "server-generated-key.pdf" && a.OwnerId == resident.Id.Value));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Throws_NotFound_When_Owner_Does_Not_Exist()
    {
        _currentUser.TenantId.Returns(Guid.NewGuid());
        _residents.GetByIdAsync(Arg.Any<ResidentId>(), Arg.Any<CancellationToken>()).Returns((Resident?)null);

        using MemoryStream content = new([1]);
        UploadAttachmentCommand command = new(
            content, nameof(AttachmentOwnerType.Resident), Guid.NewGuid(), "photo.jpg", "image/jpeg", 1);

        Func<Task> act = () => CreateHandler().Handle(command, CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<NotFoundException>();
        await _fileStorage.DidNotReceive().SaveAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Throws_NotFound_When_Owner_Belongs_To_Different_Tenant()
    {
        (Resident resident, _) = SetUpResidentOwner();
        _currentUser.TenantId.Returns(Guid.NewGuid());

        using MemoryStream content = new([1]);
        UploadAttachmentCommand command = new(
            content, nameof(AttachmentOwnerType.Resident), resident.Id.Value, "photo.jpg", "image/jpeg", 1);

        Func<Task> act = () => CreateHandler().Handle(command, CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Saves_Attachment_For_ResidentHouseholdMember_Owner()
    {
        (ResidentHouseholdMember member, _) = SetUpResidentHouseholdMemberOwner();
        _fileStorage.SaveAsync(Arg.Any<Stream>(), "photo.jpg", "image/jpeg", Arg.Any<CancellationToken>())
            .Returns("server-generated-key.jpg");

        using MemoryStream content = new([1, 2, 3]);
        UploadAttachmentCommand command = new(
            content, nameof(AttachmentOwnerType.ResidentHouseholdMember), member.Id.Value, "photo.jpg", "image/jpeg",
            3);

        await CreateHandler().Handle(command, CancellationToken.None);

        _attachments.Received(1).Add(Arg.Is<Attachment>(a =>
            a.OwnerType == AttachmentOwnerType.ResidentHouseholdMember && a.OwnerId == member.Id.Value));
    }

    [Fact]
    public async Task Saves_Attachment_For_LeasingHouseholdMember_Owner()
    {
        (HouseholdMember member, _) = SetUpLeasingHouseholdMemberOwner();
        _fileStorage.SaveAsync(Arg.Any<Stream>(), "photo.jpg", "image/jpeg", Arg.Any<CancellationToken>())
            .Returns("server-generated-key.jpg");

        using MemoryStream content = new([1, 2, 3]);
        UploadAttachmentCommand command = new(
            content, nameof(AttachmentOwnerType.LeasingHouseholdMember), member.Id.Value, "photo.jpg", "image/jpeg",
            3);

        await CreateHandler().Handle(command, CancellationToken.None);

        _attachments.Received(1).Add(Arg.Is<Attachment>(a =>
            a.OwnerType == AttachmentOwnerType.LeasingHouseholdMember && a.OwnerId == member.Id.Value));
    }

    [Fact]
    public async Task Throws_NotFound_When_ResidentHouseholdMember_Owner_Belongs_To_Different_Tenant()
    {
        (ResidentHouseholdMember member, _) = SetUpResidentHouseholdMemberOwner();
        _currentUser.TenantId.Returns(Guid.NewGuid());

        using MemoryStream content = new([1]);
        UploadAttachmentCommand command = new(
            content, nameof(AttachmentOwnerType.ResidentHouseholdMember), member.Id.Value, "photo.jpg", "image/jpeg",
            1);

        Func<Task> act = () => CreateHandler().Handle(command, CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
