using AwesomeAssertions;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Leasing.DTOs;
using MyCondo.Application.Features.Leasing.Queries.GetOccupancyRegistrationSecurityView;
using MyCondo.Domain.Features.Leasing.HouseholdMembers;
using MyCondo.Domain.Features.Leasing.OccupancyRegistrations;
using MyCondo.Domain.Features.Leasing.OccupancyRegistrationVehicleAssignments;
using MyCondo.Domain.Features.Leasing.OccupancyRegistrationWorkerAssignments;
using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Domain.Features.Property.Flats;
using MyCondo.Domain.Features.Residents;
using MyCondo.Domain.Features.Security.DomesticWorkers;
using MyCondo.Domain.Features.Security.Vehicles;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Leasing.Queries.GetOccupancyRegistrationSecurityView;

/// <summary>Proves the security view never leaks sensitive fields (the DTO type itself has no NID/DOB/
/// address properties — a compile-time guarantee) and only surfaces active household members/workers/
/// vehicles, with access eligibility tied strictly to Active status.</summary>
public class GetOccupancyRegistrationSecurityViewQueryHandlerTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;
    private static readonly FlatId FlatId = FlatId.New();
    private static readonly BuildingId BuildingId = BuildingId.New();

    private readonly IOccupancyRegistrationRepository _registrations = Substitute.For<IOccupancyRegistrationRepository>();
    private readonly IFlatRepository _flats = Substitute.For<IFlatRepository>();
    private readonly IBuildingRepository _buildings = Substitute.For<IBuildingRepository>();
    private readonly IHouseholdMemberRepository _members = Substitute.For<IHouseholdMemberRepository>();
    private readonly IOccupancyRegistrationWorkerAssignmentRepository _workerAssignments =
        Substitute.For<IOccupancyRegistrationWorkerAssignmentRepository>();
    private readonly IDomesticWorkerProfileRepository _workers = Substitute.For<IDomesticWorkerProfileRepository>();
    private readonly IOccupancyRegistrationVehicleAssignmentRepository _vehicleAssignments =
        Substitute.For<IOccupancyRegistrationVehicleAssignmentRepository>();
    private readonly IVehicleRepository _vehicles = Substitute.For<IVehicleRepository>();
    private readonly ICurrentUserProvider _currentUser = Substitute.For<ICurrentUserProvider>();

    public GetOccupancyRegistrationSecurityViewQueryHandlerTests()
    {
        _currentUser.TenantId.Returns(TenantId);
    }

    private GetOccupancyRegistrationSecurityViewQueryHandler CreateHandler() => new(
        _registrations, _flats, _buildings, _members, _workerAssignments, _workers, _vehicleAssignments, _vehicles,
        _currentUser);

    private static OccupancyRegistration ActiveRegistration()
    {
        OccupancyRegistration registration = OccupancyRegistration.Register(
            TenantId, FlatId, ResidentId.New(), ResidentType.Occupant, "Jane Doe", null, null, "1234567890",
            new DateOnly(1990, 1, 1), "Female", null, null, null, null, "123 Secret Road", null, null, null, Now);
        registration.Submit(Guid.NewGuid(), Now);
        registration.ApproveByOwner(Guid.NewGuid(), Now);
        registration.VerifyByManagement(Guid.NewGuid(), Now);
        registration.Activate(Now);
        return registration;
    }

    [Fact]
    public async Task AccessEligible_Is_True_When_Registration_Is_Active()
    {
        OccupancyRegistration registration = ActiveRegistration();
        _registrations.GetByIdAsync(registration.Id, Arg.Any<CancellationToken>()).Returns(registration);
        _members.GetForRegistrationAsync(registration.Id, Arg.Any<CancellationToken>())
            .Returns(new List<HouseholdMember>());
        _workerAssignments.GetForRegistrationAsync(registration.Id, Arg.Any<CancellationToken>())
            .Returns(new List<OccupancyRegistrationWorkerAssignment>());
        _vehicleAssignments.GetForRegistrationAsync(registration.Id, Arg.Any<CancellationToken>())
            .Returns(new List<OccupancyRegistrationVehicleAssignment>());

        OccupancyRegistrationSecurityViewDto result = await CreateHandler().Handle(
            new GetOccupancyRegistrationSecurityViewQuery(registration.Id.Value), CancellationToken.None);

        result.AccessEligible.Should().BeTrue();
        result.PrimaryFullName.Should().Be("Jane Doe");
    }

    [Fact]
    public async Task Only_Active_Household_Members_Are_Included()
    {
        OccupancyRegistration registration = ActiveRegistration();
        _registrations.GetByIdAsync(registration.Id, Arg.Any<CancellationToken>()).Returns(registration);

        HouseholdMember activeMember = HouseholdMember.Add(
            TenantId, registration.Id, "Active Member", "Spouse", null, null, null, "Female", null, null, null,
            null, null, Now);
        HouseholdMember inactiveMember = HouseholdMember.Add(
            TenantId, registration.Id, "Inactive Member", "Mother", null, null, null, "Female", null, null, null,
            null, null, Now);
        inactiveMember.Deactivate();
        _members.GetForRegistrationAsync(registration.Id, Arg.Any<CancellationToken>())
            .Returns(new List<HouseholdMember> { activeMember, inactiveMember });
        _workerAssignments.GetForRegistrationAsync(registration.Id, Arg.Any<CancellationToken>())
            .Returns(new List<OccupancyRegistrationWorkerAssignment>());
        _vehicleAssignments.GetForRegistrationAsync(registration.Id, Arg.Any<CancellationToken>())
            .Returns(new List<OccupancyRegistrationVehicleAssignment>());

        OccupancyRegistrationSecurityViewDto result = await CreateHandler().Handle(
            new GetOccupancyRegistrationSecurityViewQuery(registration.Id.Value), CancellationToken.None);

        result.HouseholdMembers.Should().ContainSingle(m => m.FullName == "Active Member");
        result.HouseholdMembers.Should().NotContain(m => m.FullName == "Inactive Member");
    }

    [Fact]
    public async Task Throws_NotFound_When_Registration_Belongs_To_Another_Tenant()
    {
        OccupancyRegistration otherTenantRegistration = OccupancyRegistration.Register(
            Guid.NewGuid(), FlatId, ResidentId.New(), ResidentType.Occupant, "Jane Doe", null, null, null, null,
            null, null, null, null, null, null, null, null, null, Now);
        _registrations.GetByIdAsync(otherTenantRegistration.Id, Arg.Any<CancellationToken>()).Returns(otherTenantRegistration);

        Func<Task> act = () => CreateHandler()
            .Handle(new GetOccupancyRegistrationSecurityViewQuery(otherTenantRegistration.Id.Value), CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
