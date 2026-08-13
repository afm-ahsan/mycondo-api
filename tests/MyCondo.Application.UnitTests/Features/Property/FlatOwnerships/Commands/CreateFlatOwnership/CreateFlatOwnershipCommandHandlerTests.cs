using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Property.FlatOwnerships.Commands.CreateFlatOwnership;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Domain.Features.Property.FlatOwnerships;
using MyCondo.Domain.Features.Property.Flats;
using MyCondo.Domain.Features.Residents;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Property.FlatOwnerships.Commands.CreateFlatOwnership;

public class CreateFlatOwnershipCommandHandlerTests
{
    private static readonly DateTimeOffset NowUtc = new(2026, 8, 13, 0, 0, 0, TimeSpan.Zero);
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid OtherTenantId = Guid.NewGuid();

    private readonly IFlatOwnershipRepository _flatOwnerships = Substitute.For<IFlatOwnershipRepository>();
    private readonly IFlatRepository _flats = Substitute.For<IFlatRepository>();
    private readonly IResidentRepository _residents = Substitute.For<IResidentRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUserProvider _currentUser = Substitute.For<ICurrentUserProvider>();
    private readonly IClock _clock = Substitute.For<IClock>();

    public CreateFlatOwnershipCommandHandlerTests()
    {
        _currentUser.TenantId.Returns(TenantId);
        _currentUser.HasPermissionForBuilding(Arg.Any<string>(), Arg.Any<Guid?>()).Returns(true);
        _clock.UtcNow.Returns(NowUtc);
    }

    private CreateFlatOwnershipCommandHandler CreateHandler() => new(
        _flatOwnerships, _flats, _residents, _unitOfWork, _currentUser, _clock,
        Substitute.For<ILogger<CreateFlatOwnershipCommandHandler>>());

    private static Flat CreateFlat(Guid tenantId) =>
        Flat.Create(tenantId, BuildingId.New(), "A-102", 2, FlatType.Residential, NowUtc);

    [Fact]
    public async Task Grants_Ownership_Of_An_Additional_Flat_To_An_Existing_Resident()
    {
        Flat flat = CreateFlat(TenantId);
        Resident resident = Resident.Register(TenantId, flat.Id, "Jane Owner", null, null, ResidentType.Owner, NowUtc);
        _flats.GetByIdAsync(flat.Id, Arg.Any<CancellationToken>()).Returns(flat);
        _residents.GetByIdAsync(resident.Id, Arg.Any<CancellationToken>()).Returns(resident);

        CreateFlatOwnershipCommand command = new(resident.Id.Value, flat.Id.Value, DateOnly.FromDateTime(NowUtc.UtcDateTime));
        CreateFlatOwnershipResult result = await CreateHandler().Handle(command, CancellationToken.None);

        result.ResidentId.Should().Be(resident.Id.Value);
        result.FlatId.Should().Be(flat.Id.Value);
        _flatOwnerships.Received(1).Add(Arg.Any<FlatOwnership>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Throws_NotFound_When_Resident_Belongs_To_A_Different_Tenant()
    {
        Flat flat = CreateFlat(TenantId);
        Resident resident = Resident.Register(OtherTenantId, flat.Id, "Jane Owner", null, null, ResidentType.Owner, NowUtc);
        _flats.GetByIdAsync(flat.Id, Arg.Any<CancellationToken>()).Returns(flat);
        _residents.GetByIdAsync(resident.Id, Arg.Any<CancellationToken>()).Returns(resident);

        CreateFlatOwnershipCommand command = new(resident.Id.Value, flat.Id.Value, DateOnly.FromDateTime(NowUtc.UtcDateTime));
        Func<Task> act = async () => await CreateHandler().Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Throws_Conflict_When_An_Active_Ownership_Already_Exists()
    {
        Flat flat = CreateFlat(TenantId);
        Resident resident = Resident.Register(TenantId, flat.Id, "Jane Owner", null, null, ResidentType.Owner, NowUtc);
        _flats.GetByIdAsync(flat.Id, Arg.Any<CancellationToken>()).Returns(flat);
        _residents.GetByIdAsync(resident.Id, Arg.Any<CancellationToken>()).Returns(resident);
        _flatOwnerships.ExistsActiveForResidentAndFlatAsync(TenantId, resident.Id.Value, flat.Id, Arg.Any<CancellationToken>())
            .Returns(true);

        CreateFlatOwnershipCommand command = new(resident.Id.Value, flat.Id.Value, DateOnly.FromDateTime(NowUtc.UtcDateTime));
        Func<Task> act = async () => await CreateHandler().Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }
}
