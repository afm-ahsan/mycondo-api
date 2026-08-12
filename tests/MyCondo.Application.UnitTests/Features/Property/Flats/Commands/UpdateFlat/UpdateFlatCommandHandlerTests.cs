using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Property.Flats.Commands.UpdateFlat;
using MyCondo.Application.Features.Property.Flats.DTOs;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Domain.Features.Property.Flats;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Property.Flats.Commands.UpdateFlat;

public class UpdateFlatCommandHandlerTests
{
    private static readonly DateTimeOffset NowUtc = new(2026, 8, 11, 0, 0, 0, TimeSpan.Zero);
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid OtherTenantId = Guid.NewGuid();
    private static readonly BuildingId TestBuildingId = BuildingId.New();

    private readonly IFlatRepository _flats = Substitute.For<IFlatRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUserProvider _currentUser = Substitute.For<ICurrentUserProvider>();

    public UpdateFlatCommandHandlerTests()
    {
        _currentUser.TenantId.Returns(TenantId);
    }

    private UpdateFlatCommandHandler CreateHandler() => new(
        _flats, _unitOfWork, _currentUser, Substitute.For<ILogger<UpdateFlatCommandHandler>>());

    [Fact]
    public async Task Updates_A_Flat_In_Callers_Tenant()
    {
        Flat flat = Flat.Create(TenantId, TestBuildingId, "A-101", 1, FlatType.Residential, NowUtc);
        _flats.GetByIdAsync(flat.Id, Arg.Any<CancellationToken>()).Returns(flat);

        FlatDto result = await CreateHandler().Handle(
            new UpdateFlatCommand(flat.Id.Value, "A-102", 2, "Commercial"), CancellationToken.None);

        result.FlatNumber.Should().Be("A-102");
        result.FloorNumber.Should().Be(2);
        result.FlatType.Should().Be("Commercial");
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Throws_Conflict_When_Flat_Number_Belongs_To_Another_Flat_In_The_Same_Building()
    {
        Flat flat = Flat.Create(TenantId, TestBuildingId, "A-101", 1, FlatType.Residential, NowUtc);
        Flat other = Flat.Create(TenantId, TestBuildingId, "A-102", 2, FlatType.Residential, NowUtc);
        _flats.GetByIdAsync(flat.Id, Arg.Any<CancellationToken>()).Returns(flat);
        _flats.GetByFlatNumberAsync(TenantId, TestBuildingId, "A-102", Arg.Any<CancellationToken>()).Returns(other);

        Func<Task> act = async () => await CreateHandler().Handle(
            new UpdateFlatCommand(flat.Id.Value, "A-102", 1, "Residential"), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Throws_NotFound_When_Flat_Belongs_To_A_Different_Tenant()
    {
        Flat flat = Flat.Create(OtherTenantId, TestBuildingId, "A-101", 1, FlatType.Residential, NowUtc);
        _flats.GetByIdAsync(flat.Id, Arg.Any<CancellationToken>()).Returns(flat);

        Func<Task> act = async () => await CreateHandler().Handle(
            new UpdateFlatCommand(flat.Id.Value, "A-101", 1, "Residential"), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
