using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Residents.HouseholdMembers.Commands.UpdateOwnerHouseholdMember;
using MyCondo.Application.Features.Residents.HouseholdMembers.DTOs;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Domain.Features.Property.Flats;
using MyCondo.Domain.Features.Residents;
using MyCondo.Domain.Features.Residents.HouseholdMembers;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Residents.HouseholdMembers.Commands.UpdateOwnerHouseholdMember;

public class UpdateOwnerHouseholdMemberCommandHandlerTests
{
    private static readonly DateTimeOffset NowUtc = new(2026, 8, 16, 0, 0, 0, TimeSpan.Zero);

    private readonly IResidentHouseholdMemberRepository _members = Substitute.For<IResidentHouseholdMemberRepository>();
    private readonly IResidentRepository _residents = Substitute.For<IResidentRepository>();
    private readonly IFlatRepository _flats = Substitute.For<IFlatRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUserProvider _currentUser = Substitute.For<ICurrentUserProvider>();
    private readonly IClock _clock = Substitute.For<IClock>();

    public UpdateOwnerHouseholdMemberCommandHandlerTests()
    {
        _clock.UtcNow.Returns(NowUtc);
    }

    private UpdateOwnerHouseholdMemberCommandHandler CreateHandler() => new(
        _members, _residents, _flats, _unitOfWork, _currentUser, _clock,
        Substitute.For<ILogger<UpdateOwnerHouseholdMemberCommandHandler>>());

    private (ResidentHouseholdMember Member, Guid TenantId, BuildingId BuildingId) SetUpMemberWithPermission(
        bool hasPermission = true)
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

        return (member, tenantId, buildingId);
    }

    [Fact]
    public async Task Updates_Member_When_Valid()
    {
        (ResidentHouseholdMember member, _, _) = SetUpMemberWithPermission();
        UpdateOwnerHouseholdMemberCommand command = new(
            member.Id.Value, "Fatema Ahmed Khan", "Spouse", "Female", new DateOnly(1992, 5, 1), null, null, "O+",
            null, null, "Doctor");

        ResidentHouseholdMemberDto result = await CreateHandler().Handle(command, CancellationToken.None);

        result.FullName.Should().Be("Fatema Ahmed Khan");
        result.Occupation.Should().Be("Doctor");
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Throws_When_Changing_To_Child_Without_Identity()
    {
        (ResidentHouseholdMember member, _, _) = SetUpMemberWithPermission();
        UpdateOwnerHouseholdMemberCommand command = new(
            member.Id.Value, "Fatema Ahmed", "Child", "Female", new DateOnly(1992, 5, 1), null, null, null, null,
            null, null);

        Func<Task> act = () => CreateHandler().Handle(command, CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task Throws_NotFound_When_Member_Does_Not_Exist()
    {
        _currentUser.TenantId.Returns(Guid.NewGuid());
        _members.GetByIdAsync(Arg.Any<ResidentHouseholdMemberId>(), Arg.Any<CancellationToken>())
            .Returns((ResidentHouseholdMember?)null);
        UpdateOwnerHouseholdMemberCommand command = new(
            Guid.NewGuid(), "Fatema Ahmed", "Spouse", "Female", new DateOnly(1992, 5, 1), null, null, null, null,
            null, null);

        Func<Task> act = () => CreateHandler().Handle(command, CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Throws_NotFound_When_Member_Belongs_To_Different_Tenant()
    {
        (ResidentHouseholdMember member, _, _) = SetUpMemberWithPermission();
        _currentUser.TenantId.Returns(Guid.NewGuid());
        UpdateOwnerHouseholdMemberCommand command = new(
            member.Id.Value, "Fatema Ahmed", "Spouse", "Female", new DateOnly(1992, 5, 1), null, null, null, null,
            null, null);

        Func<Task> act = () => CreateHandler().Handle(command, CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Throws_Forbidden_When_User_Lacks_OwnershipManage_Permission_For_Building()
    {
        (ResidentHouseholdMember member, _, _) = SetUpMemberWithPermission(hasPermission: false);
        UpdateOwnerHouseholdMemberCommand command = new(
            member.Id.Value, "Fatema Ahmed", "Spouse", "Female", new DateOnly(1992, 5, 1), null, null, null, null,
            null, null);

        Func<Task> act = () => CreateHandler().Handle(command, CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<ForbiddenException>();
    }
}
