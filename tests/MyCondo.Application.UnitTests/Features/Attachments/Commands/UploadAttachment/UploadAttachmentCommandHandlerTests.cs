using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Attachments.Commands.UploadAttachment;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Attachments;
using MyCondo.Domain.Features.Property.Flats;
using MyCondo.Domain.Features.Residents;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Attachments.Commands.UploadAttachment;

public class UploadAttachmentCommandHandlerTests
{
    private static readonly DateTimeOffset NowUtc = new(2026, 8, 16, 0, 0, 0, TimeSpan.Zero);

    private readonly IAttachmentRepository _attachments = Substitute.For<IAttachmentRepository>();
    private readonly IResidentRepository _residents = Substitute.For<IResidentRepository>();
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
}
