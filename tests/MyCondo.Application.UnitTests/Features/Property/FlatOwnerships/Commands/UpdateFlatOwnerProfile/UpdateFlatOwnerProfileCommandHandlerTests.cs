using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Property.FlatOwnerships.Commands.UpdateFlatOwnerProfile;
using MyCondo.Application.Features.Residents.DTOs;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Domain.Features.Property.Flats;
using MyCondo.Domain.Features.Residents;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Property.FlatOwnerships.Commands.UpdateFlatOwnerProfile;

public class UpdateFlatOwnerProfileCommandHandlerTests
{
    private static readonly DateTimeOffset NowUtc = new(2026, 8, 13, 0, 0, 0, TimeSpan.Zero);
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid OtherTenantId = Guid.NewGuid();

    private readonly IResidentRepository _residents = Substitute.For<IResidentRepository>();
    private readonly IFlatRepository _flats = Substitute.For<IFlatRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUserProvider _currentUser = Substitute.For<ICurrentUserProvider>();
    private readonly IClock _clock = Substitute.For<IClock>();

    public UpdateFlatOwnerProfileCommandHandlerTests()
    {
        _currentUser.TenantId.Returns(TenantId);
        _currentUser.HasPermissionForBuilding(Arg.Any<string>(), Arg.Any<Guid?>()).Returns(true);
        _clock.UtcNow.Returns(NowUtc);
    }

    private UpdateFlatOwnerProfileCommandHandler CreateHandler() => new(
        _residents, _flats, _unitOfWork, _currentUser, _clock,
        Substitute.For<ILogger<UpdateFlatOwnerProfileCommandHandler>>());

    private static UpdateFlatOwnerProfileCommand BuildCommand(Guid residentId) => new(
        residentId, "Updated Name", "01700000000", "updated@example.com", "01711111111", "9876543210", "P7654321",
        new DateOnly(1985, 5, 5), "Male", "New Present Addr", "New Permanent Addr", "New Father", "New Mother",
        "Single", "Doctor", "New Employer", "New Office Addr", "New Emergency Contact", "01799999999");

    [Fact]
    public async Task Updates_Base_And_Extended_Profile_Fields()
    {
        Flat flat = Flat.Create(TenantId, BuildingId.New(), "A-101", 1, FlatType.Residential, NowUtc);
        Resident resident = Resident.Register(TenantId, flat.Id, "Original Name", null, null, ResidentType.Owner, NowUtc);
        _residents.GetByIdAsync(resident.Id, Arg.Any<CancellationToken>()).Returns(resident);
        _flats.GetByIdAsync(flat.Id, Arg.Any<CancellationToken>()).Returns(flat);

        ResidentDto result = await CreateHandler().Handle(BuildCommand(resident.Id.Value), CancellationToken.None);

        result.FullName.Should().Be("Updated Name");
        result.Profession.Should().Be("Doctor");
        result.MaritalStatus.Should().Be("Single");
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Throws_NotFound_When_Resident_Belongs_To_A_Different_Tenant()
    {
        Flat flat = Flat.Create(OtherTenantId, BuildingId.New(), "A-101", 1, FlatType.Residential, NowUtc);
        Resident resident = Resident.Register(OtherTenantId, flat.Id, "Original Name", null, null, ResidentType.Owner, NowUtc);
        _residents.GetByIdAsync(resident.Id, Arg.Any<CancellationToken>()).Returns(resident);

        Func<Task> act = async () => await CreateHandler().Handle(BuildCommand(resident.Id.Value), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Throws_Forbidden_When_Caller_Lacks_Ownership_Manage_For_The_Building()
    {
        Flat flat = Flat.Create(TenantId, BuildingId.New(), "A-101", 1, FlatType.Residential, NowUtc);
        Resident resident = Resident.Register(TenantId, flat.Id, "Original Name", null, null, ResidentType.Owner, NowUtc);
        _residents.GetByIdAsync(resident.Id, Arg.Any<CancellationToken>()).Returns(resident);
        _flats.GetByIdAsync(flat.Id, Arg.Any<CancellationToken>()).Returns(flat);
        _currentUser.HasPermissionForBuilding("ownership.manage", flat.BuildingId.Value).Returns(false);

        Func<Task> act = async () => await CreateHandler().Handle(BuildCommand(resident.Id.Value), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }
}
