using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Leasing.Commands.CreateOccupancyRegistration;
using MyCondo.Application.Features.Leasing.DTOs;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Leasing.OccupancyRegistrations;
using MyCondo.Domain.Features.Leasing.OccupancyRegistrationStatusHistories;
using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Domain.Features.Property.Flats;
using MyCondo.Domain.Features.Residents;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Leasing.Commands.CreateOccupancyRegistration;

/// <summary>Proves the "reuse the shared Resident party record instead of duplicating it" requirement,
/// and that a flat with an existing Active registration cannot take a second one.</summary>
public class CreateOccupancyRegistrationCommandHandlerTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly FlatId FlatId = FlatId.New();
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    private readonly IOccupancyRegistrationRepository _registrations = Substitute.For<IOccupancyRegistrationRepository>();
    private readonly IOccupancyRegistrationStatusHistoryRepository _history = Substitute.For<IOccupancyRegistrationStatusHistoryRepository>();
    private readonly IFlatRepository _flats = Substitute.For<IFlatRepository>();
    private readonly IResidentRepository _residents = Substitute.For<IResidentRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUserProvider _currentUser = Substitute.For<ICurrentUserProvider>();
    private readonly IClock _clock = Substitute.For<IClock>();

    public CreateOccupancyRegistrationCommandHandlerTests()
    {
        _currentUser.TenantId.Returns(TenantId);
        _clock.UtcNow.Returns(Now);
    }

    private CreateOccupancyRegistrationCommandHandler CreateHandler() => new(
        _registrations, _history, _flats, _residents, _unitOfWork, _currentUser, _clock,
        Substitute.For<ILogger<CreateOccupancyRegistrationCommandHandler>>());

    private static Flat ActiveFlat() => Flat.Create(TenantId, BuildingId.New(), "A-101", 1, FlatType.Residential, Now);

    private static CreateOccupancyRegistrationCommand ValidCommand() => new(
        FlatId.Value, "Occupant", "Jane Doe", "01700000000", "jane@example.com", "1234567890",
        new DateOnly(1990, 1, 1), "123 Example Road, Dhaka", "John Doe", "01711111111", null);

    [Fact]
    public async Task Reuses_Existing_Resident_When_Name_Matches_On_The_Flat()
    {
        _flats.GetByIdAsync(FlatId, Arg.Any<CancellationToken>()).Returns(ActiveFlat());
        _registrations.GetActiveForFlatAsync(TenantId, FlatId, Arg.Any<CancellationToken>()).Returns((OccupancyRegistration?)null);
        Resident existing = Resident.Register(TenantId, FlatId, "Jane Doe", "01700000000", "jane@example.com", ResidentType.Occupant, Now);
        _residents.FindByFlatAndNameAsync(TenantId, FlatId, "Jane Doe", Arg.Any<CancellationToken>()).Returns(existing);

        OccupancyRegistrationDto result = await CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        result.PrimaryResidentId.Should().Be(existing.Id.Value);
        _residents.DidNotReceive().Add(Arg.Any<Resident>());
    }

    [Fact]
    public async Task Creates_New_Resident_When_No_Match_Exists()
    {
        _flats.GetByIdAsync(FlatId, Arg.Any<CancellationToken>()).Returns(ActiveFlat());
        _registrations.GetActiveForFlatAsync(TenantId, FlatId, Arg.Any<CancellationToken>()).Returns((OccupancyRegistration?)null);
        _residents.FindByFlatAndNameAsync(TenantId, FlatId, "Jane Doe", Arg.Any<CancellationToken>()).Returns((Resident?)null);

        OccupancyRegistrationDto result = await CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        result.Status.Should().Be(nameof(OccupancyRegistrationStatus.Draft));
        _residents.Received(1).Add(Arg.Any<Resident>());
    }

    [Fact]
    public async Task Throws_When_Flat_Already_Has_An_Active_Registration()
    {
        _flats.GetByIdAsync(FlatId, Arg.Any<CancellationToken>()).Returns(ActiveFlat());
        OccupancyRegistration existingActive = OccupancyRegistration.Register(
            TenantId, FlatId, ResidentId.New(), ResidentType.Owner, "Existing Occupant", null, null, null, null, null,
            null, null, null, Now);
        _registrations.GetActiveForFlatAsync(TenantId, FlatId, Arg.Any<CancellationToken>()).Returns(existingActive);

        Func<Task> act = () => CreateHandler().Handle(ValidCommand(), CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Throws_NotFound_When_Flat_Belongs_To_Another_Tenant()
    {
        Flat otherTenantFlat = Flat.Create(Guid.NewGuid(), BuildingId.New(), "A-101", 1, FlatType.Residential, Now);
        _flats.GetByIdAsync(FlatId, Arg.Any<CancellationToken>()).Returns(otherTenantFlat);

        Func<Task> act = () => CreateHandler().Handle(ValidCommand(), CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
