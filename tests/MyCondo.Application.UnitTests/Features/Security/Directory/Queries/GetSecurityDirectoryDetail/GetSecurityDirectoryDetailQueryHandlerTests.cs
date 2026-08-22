using AwesomeAssertions;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Security.Directory.DTOs;
using MyCondo.Application.Features.Security.Directory.Queries.GetSecurityDirectoryDetail;
using MyCondo.Domain.Features.Leasing.HouseholdMembers;
using MyCondo.Domain.Features.Leasing.OccupancyRegistrations;
using MyCondo.Domain.Features.Leasing.OccupancyRegistrationVehicleAssignments;
using MyCondo.Domain.Features.Leasing.OccupancyRegistrationWorkerAssignments;
using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Domain.Features.Property.FlatOwnerships;
using MyCondo.Domain.Features.Property.Flats;
using MyCondo.Domain.Features.Residents;
using MyCondo.Domain.Features.Residents.HouseholdMembers;
using MyCondo.Domain.Features.Security.DomesticWorkers;
using MyCondo.Domain.Features.Security.Vehicles;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Security.Directory.Queries.GetSecurityDirectoryDetail;

/// <summary>Proves the merged detail view never leaks sensitive fields (the DTO type itself has no NID/
/// DOB/address properties — a compile-time guarantee), ties access status strictly to Active status for
/// both Owner and Tenant entries, and gates each optional section on its own granular permission
/// (null = not authorized, distinct from an authorized-but-empty list).</summary>
public class GetSecurityDirectoryDetailQueryHandlerTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;
    private static readonly FlatId FlatId = FlatId.New();

    private readonly IOccupancyRegistrationRepository _registrations = Substitute.For<IOccupancyRegistrationRepository>();
    private readonly IFlatOwnershipRepository _flatOwnerships = Substitute.For<IFlatOwnershipRepository>();
    private readonly IResidentRepository _residents = Substitute.For<IResidentRepository>();
    private readonly IFlatRepository _flats = Substitute.For<IFlatRepository>();
    private readonly IBuildingRepository _buildings = Substitute.For<IBuildingRepository>();
    private readonly IHouseholdMemberRepository _members = Substitute.For<IHouseholdMemberRepository>();
    private readonly IResidentHouseholdMemberRepository _residentMembers = Substitute.For<IResidentHouseholdMemberRepository>();
    private readonly IOccupancyRegistrationWorkerAssignmentRepository _workerAssignments =
        Substitute.For<IOccupancyRegistrationWorkerAssignmentRepository>();
    private readonly IDomesticWorkerProfileRepository _workers = Substitute.For<IDomesticWorkerProfileRepository>();
    private readonly IOccupancyRegistrationVehicleAssignmentRepository _vehicleAssignments =
        Substitute.For<IOccupancyRegistrationVehicleAssignmentRepository>();
    private readonly IVehicleRepository _vehicles = Substitute.For<IVehicleRepository>();
    private readonly ICurrentUserProvider _currentUser = Substitute.For<ICurrentUserProvider>();

    public GetSecurityDirectoryDetailQueryHandlerTests()
    {
        _currentUser.TenantId.Returns(TenantId);
    }

    private GetSecurityDirectoryDetailQueryHandler CreateHandler() => new(
        _registrations, _flatOwnerships, _residents, _flats, _buildings, _members, _residentMembers,
        _workerAssignments, _workers, _vehicleAssignments, _vehicles, _currentUser);

    private static OccupancyRegistration ActiveRegistration()
    {
        OccupancyRegistration registration = OccupancyRegistration.Register(
            TenantId, FlatId, ResidentId.New(), ResidentType.Occupant, "Jane Doe", "01700000000", null,
            "1234567890", new DateOnly(1990, 1, 1), "Female", null, null, null, null, null, null, null,
            "123 Secret Road", null, null, null, Now);
        registration.Submit(Guid.NewGuid(), Now);
        registration.ApproveByOwner(Guid.NewGuid(), Now);
        registration.VerifyByManagement(Guid.NewGuid(), Now);
        registration.Activate(Now);
        return registration;
    }

    [Fact]
    public async Task Tenant_Entry_Is_Authorized_When_Registration_Is_Active()
    {
        OccupancyRegistration registration = ActiveRegistration();
        _registrations.GetByIdAsync(registration.Id, Arg.Any<CancellationToken>()).Returns(registration);

        SecurityDirectoryDetailDto result = await CreateHandler().Handle(
            new GetSecurityDirectoryDetailQuery(registration.Id.Value, "Tenant"), CancellationToken.None);

        result.AccessStatus.Should().Be("Authorized");
        result.ResidentType.Should().Be("Tenant");
        result.PrimaryFullName.Should().Be("Jane Doe");
        result.PrimaryPhone.Should().Be("+8801700000000");
    }

    [Fact]
    public async Task Tenant_Sections_Are_Null_Without_The_Granular_Permission()
    {
        OccupancyRegistration registration = ActiveRegistration();
        _registrations.GetByIdAsync(registration.Id, Arg.Any<CancellationToken>()).Returns(registration);
        _currentUser.HasPermission(Arg.Any<string>()).Returns(false);

        SecurityDirectoryDetailDto result = await CreateHandler().Handle(
            new GetSecurityDirectoryDetailQuery(registration.Id.Value, "Tenant"), CancellationToken.None);

        result.HouseholdMembers.Should().BeNull();
        result.Workers.Should().BeNull();
        result.Vehicles.Should().BeNull();
        result.ExtendedDetail.Should().BeNull();
    }

    [Fact]
    public async Task Tenant_Household_Section_Is_Populated_And_Only_Active_Members_Included_When_Authorized()
    {
        OccupancyRegistration registration = ActiveRegistration();
        _registrations.GetByIdAsync(registration.Id, Arg.Any<CancellationToken>()).Returns(registration);
        _currentUser.HasPermission("security.directory.household.view").Returns(true);

        HouseholdMember activeMember = HouseholdMember.Add(
            TenantId, registration.Id, "Active Member", "Spouse", null, null, null, "Female", null, null, null,
            null, null, Now);
        HouseholdMember inactiveMember = HouseholdMember.Add(
            TenantId, registration.Id, "Inactive Member", "Mother", null, null, null, "Female", null, null, null,
            null, null, Now);
        inactiveMember.Deactivate();
        _members.GetForRegistrationAsync(registration.Id, Arg.Any<CancellationToken>())
            .Returns(new List<HouseholdMember> { activeMember, inactiveMember });

        SecurityDirectoryDetailDto result = await CreateHandler().Handle(
            new GetSecurityDirectoryDetailQuery(registration.Id.Value, "Tenant"), CancellationToken.None);

        result.HouseholdMembers.Should().ContainSingle(m => m.FullName == "Active Member");
        result.HouseholdMembers.Should().NotContain(m => m.FullName == "Inactive Member");
    }

    [Fact]
    public async Task Throws_NotFound_When_Tenant_Registration_Belongs_To_Another_Tenant()
    {
        OccupancyRegistration otherTenantRegistration = OccupancyRegistration.Register(
            Guid.NewGuid(), FlatId, ResidentId.New(), ResidentType.Occupant, "Jane Doe", null, null, null, null,
            null, null, null, null, null, null, null, null, null, null, null, null, Now);
        _registrations.GetByIdAsync(otherTenantRegistration.Id, Arg.Any<CancellationToken>())
            .Returns(otherTenantRegistration);

        Func<Task> act = () => CreateHandler()
            .Handle(new GetSecurityDirectoryDetailQuery(otherTenantRegistration.Id.Value, "Tenant"), CancellationToken.None)
            .AsTask();

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Owner_Entry_Is_Revoked_When_Ownership_Has_Ended()
    {
        Resident owner = Resident.Register(TenantId, FlatId, "John Owner", "01800000000", null, ResidentType.Owner, Now);
        FlatOwnership ownership = FlatOwnership.Grant(TenantId, owner.Id.Value, FlatId, new DateOnly(2020, 1, 1), Now);
        ownership.End(new DateOnly(2024, 1, 1), Now);

        _flatOwnerships.GetByIdAsync(ownership.Id, Arg.Any<CancellationToken>()).Returns(ownership);
        _residents.GetByIdAsync(owner.Id, Arg.Any<CancellationToken>()).Returns(owner);

        SecurityDirectoryDetailDto result = await CreateHandler().Handle(
            new GetSecurityDirectoryDetailQuery(ownership.Id.Value, "Owner"), CancellationToken.None);

        result.AccessStatus.Should().Be("Revoked");
        result.ResidentType.Should().Be("Owner");
        result.PrimaryFullName.Should().Be("John Owner");
    }

    [Fact]
    public async Task Owner_Worker_And_Vehicle_Sections_Are_Empty_Not_Null_When_Authorized()
    {
        Resident owner = Resident.Register(TenantId, FlatId, "John Owner", null, null, ResidentType.Owner, Now);
        FlatOwnership ownership = FlatOwnership.Grant(TenantId, owner.Id.Value, FlatId, new DateOnly(2020, 1, 1), Now);
        _flatOwnerships.GetByIdAsync(ownership.Id, Arg.Any<CancellationToken>()).Returns(ownership);
        _residents.GetByIdAsync(owner.Id, Arg.Any<CancellationToken>()).Returns(owner);
        _currentUser.HasPermission("security.directory.worker.view").Returns(true);
        _currentUser.HasPermission("security.directory.vehicle.view").Returns(true);

        SecurityDirectoryDetailDto result = await CreateHandler().Handle(
            new GetSecurityDirectoryDetailQuery(ownership.Id.Value, "Owner"), CancellationToken.None);

        result.Workers.Should().NotBeNull().And.BeEmpty();
        result.Vehicles.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public async Task Throws_NotFound_When_Ownership_Belongs_To_Another_Tenant()
    {
        FlatOwnership otherTenantOwnership = FlatOwnership.Grant(Guid.NewGuid(), Guid.NewGuid(), FlatId, new DateOnly(2020, 1, 1), Now);
        _flatOwnerships.GetByIdAsync(otherTenantOwnership.Id, Arg.Any<CancellationToken>()).Returns(otherTenantOwnership);

        Func<Task> act = () => CreateHandler()
            .Handle(new GetSecurityDirectoryDetailQuery(otherTenantOwnership.Id.Value, "Owner"), CancellationToken.None)
            .AsTask();

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
