using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Leasing.Commands.AddHouseholdMember;
using MyCondo.Application.Features.Leasing.DTOs;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Leasing.HouseholdMembers;
using MyCondo.Domain.Features.Leasing.OccupancyRegistrations;
using MyCondo.Domain.Features.Property.Flats;
using MyCondo.Domain.Features.Residents;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Leasing.Commands.AddHouseholdMember;

public class AddHouseholdMemberCommandHandlerTests
{
    private static readonly DateTimeOffset NowUtc = new(2026, 8, 16, 0, 0, 0, TimeSpan.Zero);

    private readonly IOccupancyRegistrationRepository _registrations = Substitute.For<IOccupancyRegistrationRepository>();
    private readonly IHouseholdMemberRepository _members = Substitute.For<IHouseholdMemberRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUserProvider _currentUser = Substitute.For<ICurrentUserProvider>();
    private readonly IClock _clock = Substitute.For<IClock>();

    public AddHouseholdMemberCommandHandlerTests()
    {
        _clock.UtcNow.Returns(NowUtc);
    }

    private AddHouseholdMemberCommandHandler CreateHandler() => new(
        _registrations, _members, _unitOfWork, _currentUser, _clock,
        Substitute.For<ILogger<AddHouseholdMemberCommandHandler>>());

    private (OccupancyRegistration Registration, Guid TenantId) SetUpRegistration()
    {
        Guid tenantId = Guid.NewGuid();
        OccupancyRegistration registration = OccupancyRegistration.Register(
            tenantId, FlatId.New(), ResidentId.New(), ResidentType.Occupant, "John Doe", null, null, null, null,
            null, null, null, null, null, null, null, null, null, NowUtc);

        _currentUser.TenantId.Returns(tenantId);
        _registrations.GetByIdAsync(registration.Id, Arg.Any<CancellationToken>()).Returns(registration);

        return (registration, tenantId);
    }

    [Fact]
    public async Task Adds_Member_When_Valid()
    {
        (OccupancyRegistration registration, _) = SetUpRegistration();
        AddHouseholdMemberCommand command = new(
            registration.Id.Value, "Jane Doe", "Spouse", new DateOnly(1992, 5, 1), null, null, "Female", null, null,
            null, null, null);

        HouseholdMemberDto result = await CreateHandler().Handle(command, CancellationToken.None);

        result.FullName.Should().Be("Jane Doe");
        _members.Received(1).Add(Arg.Any<HouseholdMember>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Throws_When_Child_Has_Neither_NationalId_Nor_BirthCertificate()
    {
        (OccupancyRegistration registration, _) = SetUpRegistration();
        AddHouseholdMemberCommand command = new(
            registration.Id.Value, "Baby Doe", "Child", new DateOnly(2020, 1, 1), null, null, "Female", null, null,
            null, null, null);

        Func<Task> act = () => CreateHandler().Handle(command, CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task Throws_NotFound_When_Registration_Does_Not_Exist()
    {
        _currentUser.TenantId.Returns(Guid.NewGuid());
        _registrations.GetByIdAsync(Arg.Any<OccupancyRegistrationId>(), Arg.Any<CancellationToken>())
            .Returns((OccupancyRegistration?)null);
        AddHouseholdMemberCommand command = new(
            Guid.NewGuid(), "Jane Doe", "Spouse", new DateOnly(1992, 5, 1), null, null, "Female", null, null, null,
            null, null);

        Func<Task> act = () => CreateHandler().Handle(command, CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Throws_NotFound_When_Registration_Belongs_To_Different_Tenant()
    {
        (OccupancyRegistration registration, _) = SetUpRegistration();
        _currentUser.TenantId.Returns(Guid.NewGuid());
        AddHouseholdMemberCommand command = new(
            registration.Id.Value, "Jane Doe", "Spouse", new DateOnly(1992, 5, 1), null, null, "Female", null, null,
            null, null, null);

        Func<Task> act = () => CreateHandler().Handle(command, CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Throws_Conflict_When_Registration_Is_Rejected()
    {
        Guid tenantId = Guid.NewGuid();
        OccupancyRegistration registration = OccupancyRegistration.Register(
            tenantId, FlatId.New(), ResidentId.New(), ResidentType.Occupant, "John Doe", null, null, "1234567890",
            new DateOnly(1990, 1, 1), "Male", null, null, null, null, null, null, null, null, NowUtc);
        registration.Submit(null, NowUtc);
        registration.Reject("not eligible", NowUtc);
        _currentUser.TenantId.Returns(tenantId);
        _registrations.GetByIdAsync(registration.Id, Arg.Any<CancellationToken>()).Returns(registration);

        AddHouseholdMemberCommand command = new(
            registration.Id.Value, "Jane Doe", "Spouse", new DateOnly(1992, 5, 1), null, null, "Female", null, null,
            null, null, null);

        Func<Task> act = () => CreateHandler().Handle(command, CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<ConflictException>();
    }
}
