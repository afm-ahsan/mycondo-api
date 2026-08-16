using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Residents.HouseholdMembers.Commands.AddOwnerHouseholdMember;
using MyCondo.Application.Features.Residents.HouseholdMembers.DTOs;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Domain.Features.Property.Flats;
using MyCondo.Domain.Features.Residents;
using MyCondo.Domain.Features.Residents.HouseholdMembers;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Residents.HouseholdMembers.Commands.AddOwnerHouseholdMember;

public class AddOwnerHouseholdMemberCommandHandlerTests
{
    private static readonly DateTimeOffset NowUtc = new(2026, 8, 16, 0, 0, 0, TimeSpan.Zero);

    private readonly IResidentRepository _residents = Substitute.For<IResidentRepository>();
    private readonly IFlatRepository _flats = Substitute.For<IFlatRepository>();
    private readonly IResidentHouseholdMemberRepository _members = Substitute.For<IResidentHouseholdMemberRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUserProvider _currentUser = Substitute.For<ICurrentUserProvider>();
    private readonly IClock _clock = Substitute.For<IClock>();

    public AddOwnerHouseholdMemberCommandHandlerTests()
    {
        _clock.UtcNow.Returns(NowUtc);
    }

    private AddOwnerHouseholdMemberCommandHandler CreateHandler() => new(
        _residents, _flats, _members, _unitOfWork, _currentUser, _clock,
        Substitute.For<ILogger<AddOwnerHouseholdMemberCommandHandler>>());

    private (Resident Resident, Guid TenantId) SetUpOwnerWithPermission(bool hasPermission = true)
    {
        Guid tenantId = Guid.NewGuid();
        BuildingId buildingId = BuildingId.New();
        Flat flat = Flat.Create(tenantId, buildingId, "A-1", 1, FlatType.Residential, NowUtc);
        Resident resident = Resident.Register(tenantId, flat.Id, "Jane Doe", null, null, ResidentType.Owner, NowUtc);

        _currentUser.TenantId.Returns(tenantId);
        _currentUser.HasPermissionForBuilding("ownership.manage", buildingId.Value).Returns(hasPermission);
        _residents.GetByIdAsync(resident.Id, Arg.Any<CancellationToken>()).Returns(resident);
        _flats.GetByIdAsync(flat.Id, Arg.Any<CancellationToken>()).Returns(flat);

        return (resident, tenantId);
    }

    [Fact]
    public async Task Adds_Member_When_Valid()
    {
        (Resident resident, _) = SetUpOwnerWithPermission();
        AddOwnerHouseholdMemberCommand command = new(
            resident.Id.Value, "Fatema Ahmed", "Spouse", "Female", new DateOnly(1992, 5, 1), null, null, null, null,
            null, null);

        ResidentHouseholdMemberDto result = await CreateHandler().Handle(command, CancellationToken.None);

        result.FullName.Should().Be("Fatema Ahmed");
        _members.Received(1).Add(Arg.Any<ResidentHouseholdMember>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Throws_When_Child_Has_Neither_NationalId_Nor_BirthCertificate()
    {
        (Resident resident, _) = SetUpOwnerWithPermission();
        AddOwnerHouseholdMemberCommand command = new(
            resident.Id.Value, "Baby Doe", "Child", "Female", new DateOnly(2020, 1, 1), null, null, null, null,
            null, null);

        Func<Task> act = () => CreateHandler().Handle(command, CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task Throws_When_DateOfBirth_Is_In_The_Future()
    {
        (Resident resident, _) = SetUpOwnerWithPermission();
        AddOwnerHouseholdMemberCommand command = new(
            resident.Id.Value, "Fatema Ahmed", "Spouse", "Female",
            DateOnly.FromDateTime(NowUtc.UtcDateTime).AddDays(1), null, null, null, null, null, null);

        Func<Task> act = () => CreateHandler().Handle(command, CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task Throws_NotFound_When_Resident_Does_Not_Exist()
    {
        _currentUser.TenantId.Returns(Guid.NewGuid());
        _residents.GetByIdAsync(Arg.Any<ResidentId>(), Arg.Any<CancellationToken>()).Returns((Resident?)null);
        AddOwnerHouseholdMemberCommand command = new(
            Guid.NewGuid(), "Fatema Ahmed", "Spouse", "Female", new DateOnly(1992, 5, 1), null, null, null, null,
            null, null);

        Func<Task> act = () => CreateHandler().Handle(command, CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Throws_NotFound_When_Resident_Belongs_To_Different_Tenant()
    {
        (Resident resident, _) = SetUpOwnerWithPermission();
        _currentUser.TenantId.Returns(Guid.NewGuid());
        AddOwnerHouseholdMemberCommand command = new(
            resident.Id.Value, "Fatema Ahmed", "Spouse", "Female", new DateOnly(1992, 5, 1), null, null, null, null,
            null, null);

        Func<Task> act = () => CreateHandler().Handle(command, CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Throws_Forbidden_When_User_Lacks_OwnershipManage_Permission_For_Building()
    {
        (Resident resident, _) = SetUpOwnerWithPermission(hasPermission: false);
        AddOwnerHouseholdMemberCommand command = new(
            resident.Id.Value, "Fatema Ahmed", "Spouse", "Female", new DateOnly(1992, 5, 1), null, null, null, null,
            null, null);

        Func<Task> act = () => CreateHandler().Handle(command, CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<ForbiddenException>();
    }
}
