using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Property.FlatOwnerships.Commands.RegisterFlatOwner;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Domain.Features.Property.FlatOwnerships;
using MyCondo.Domain.Features.Property.Flats;
using MyCondo.Domain.Features.Residents;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Property.FlatOwnerships.Commands.RegisterFlatOwner;

public class RegisterFlatOwnerCommandHandlerTests
{
    private static readonly DateTimeOffset NowUtc = new(2026, 8, 13, 0, 0, 0, TimeSpan.Zero);
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid OtherTenantId = Guid.NewGuid();

    private readonly IResidentRepository _residents = Substitute.For<IResidentRepository>();
    private readonly IFlatOwnershipRepository _flatOwnerships = Substitute.For<IFlatOwnershipRepository>();
    private readonly IFlatRepository _flats = Substitute.For<IFlatRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUserProvider _currentUser = Substitute.For<ICurrentUserProvider>();
    private readonly IClock _clock = Substitute.For<IClock>();

    public RegisterFlatOwnerCommandHandlerTests()
    {
        _currentUser.TenantId.Returns(TenantId);
        _currentUser.HasPermissionForBuilding(Arg.Any<string>(), Arg.Any<Guid?>()).Returns(true);
        _clock.UtcNow.Returns(NowUtc);
    }

    private RegisterFlatOwnerCommandHandler CreateHandler() => new(
        _residents, _flatOwnerships, _flats, _unitOfWork, _currentUser, _clock,
        Substitute.For<ILogger<RegisterFlatOwnerCommandHandler>>());

    private static Flat CreateFlat(Guid tenantId) =>
        Flat.Create(tenantId, BuildingId.New(), "A-101", 1, FlatType.Residential, NowUtc);

    private static RegisterFlatOwnerCommand BuildCommand(Guid flatId) => new(
        flatId, DateOnly.FromDateTime(NowUtc.UtcDateTime), "Jane Owner", "01700000000", "jane@example.com",
        "01711111111", "1234567890123", "P1234567", new DateOnly(1990, 1, 1), "Female", "Present Addr",
        "Permanent Addr", "Father Name", "Mother Name", "Married", "Engineer", "Acme Corp", "Office Addr",
        "Emergency Contact", "01788888888");

    [Fact]
    public async Task Registers_A_New_Resident_And_Grants_The_First_Ownership()
    {
        Flat flat = CreateFlat(TenantId);
        _flats.GetByIdAsync(flat.Id, Arg.Any<CancellationToken>()).Returns(flat);
        _residents.FindByFlatAndNameAsync(TenantId, flat.Id, "Jane Owner", Arg.Any<CancellationToken>())
            .Returns((Resident?)null);

        RegisterFlatOwnerResult result = await CreateHandler().Handle(BuildCommand(flat.Id.Value), CancellationToken.None);

        result.Resident.FullName.Should().Be("Jane Owner");
        result.Resident.ResidentType.Should().Be(nameof(ResidentType.Owner));
        result.Resident.NationalIdNumberMasked.Should().NotBe("1234567890123", "the raw NID must never be returned unmasked");
        _residents.Received(1).Add(Arg.Any<Resident>());
        _flatOwnerships.Received(1).Add(Arg.Any<FlatOwnership>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Reuses_An_Existing_Resident_Already_On_File_For_The_Flat_Instead_Of_Duplicating()
    {
        Flat flat = CreateFlat(TenantId);
        Resident existing = Resident.Register(TenantId, flat.Id, "Jane Owner", null, null, ResidentType.Occupant, NowUtc);
        _flats.GetByIdAsync(flat.Id, Arg.Any<CancellationToken>()).Returns(flat);
        _residents.FindByFlatAndNameAsync(TenantId, flat.Id, "Jane Owner", Arg.Any<CancellationToken>()).Returns(existing);

        RegisterFlatOwnerResult result = await CreateHandler().Handle(BuildCommand(flat.Id.Value), CancellationToken.None);

        result.ResidentId.Should().Be(existing.Id.Value);
        _residents.DidNotReceive().Add(Arg.Any<Resident>());
    }

    [Fact]
    public async Task Throws_Conflict_When_Resident_Already_Has_An_Active_Ownership_For_The_Flat()
    {
        Flat flat = CreateFlat(TenantId);
        Resident existing = Resident.Register(TenantId, flat.Id, "Jane Owner", null, null, ResidentType.Owner, NowUtc);
        _flats.GetByIdAsync(flat.Id, Arg.Any<CancellationToken>()).Returns(flat);
        _residents.FindByFlatAndNameAsync(TenantId, flat.Id, "Jane Owner", Arg.Any<CancellationToken>()).Returns(existing);
        _flatOwnerships.ExistsActiveForResidentAndFlatAsync(TenantId, existing.Id.Value, flat.Id, Arg.Any<CancellationToken>())
            .Returns(true);

        Func<Task> act = async () => await CreateHandler().Handle(BuildCommand(flat.Id.Value), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Throws_NotFound_When_Flat_Belongs_To_A_Different_Tenant()
    {
        Flat flat = CreateFlat(OtherTenantId);
        _flats.GetByIdAsync(flat.Id, Arg.Any<CancellationToken>()).Returns(flat);

        Func<Task> act = async () => await CreateHandler().Handle(BuildCommand(flat.Id.Value), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Throws_Forbidden_When_Caller_Lacks_Ownership_Manage_For_The_Building()
    {
        Flat flat = CreateFlat(TenantId);
        _flats.GetByIdAsync(flat.Id, Arg.Any<CancellationToken>()).Returns(flat);
        _currentUser.HasPermissionForBuilding("ownership.manage", flat.BuildingId.Value).Returns(false);

        Func<Task> act = async () => await CreateHandler().Handle(BuildCommand(flat.Id.Value), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }
}
