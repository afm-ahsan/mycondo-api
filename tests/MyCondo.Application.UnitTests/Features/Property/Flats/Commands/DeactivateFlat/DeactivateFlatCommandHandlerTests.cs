using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Property.Flats.Commands.DeactivateFlat;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Domain.Features.Property.Flats;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Property.Flats.Commands.DeactivateFlat;

public class DeactivateFlatCommandHandlerTests
{
    private static readonly DateTimeOffset NowUtc = new(2026, 8, 11, 0, 0, 0, TimeSpan.Zero);
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid OtherTenantId = Guid.NewGuid();
    private static readonly BuildingId TestBuildingId = BuildingId.New();

    private readonly IFlatRepository _flats = Substitute.For<IFlatRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUserProvider _currentUser = Substitute.For<ICurrentUserProvider>();
    private readonly IClock _clock = Substitute.For<IClock>();

    public DeactivateFlatCommandHandlerTests()
    {
        _currentUser.TenantId.Returns(TenantId);
        _clock.UtcNow.Returns(NowUtc);
    }

    private DeactivateFlatCommandHandler CreateHandler() => new(
        _flats, _unitOfWork, _currentUser, _clock, Substitute.For<ILogger<DeactivateFlatCommandHandler>>());

    [Fact]
    public async Task Deactivates_A_Flat_In_Callers_Tenant()
    {
        Flat flat = Flat.Create(TenantId, TestBuildingId, "A-101", 1, FlatType.Residential, NowUtc);
        _flats.GetByIdAsync(flat.Id, Arg.Any<CancellationToken>()).Returns(flat);

        await CreateHandler().Handle(new DeactivateFlatCommand(flat.Id.Value), CancellationToken.None);

        flat.DeletedAtUtc.Should().Be(NowUtc);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Throws_NotFound_When_Flat_Belongs_To_A_Different_Tenant()
    {
        Flat flat = Flat.Create(OtherTenantId, TestBuildingId, "A-101", 1, FlatType.Residential, NowUtc);
        _flats.GetByIdAsync(flat.Id, Arg.Any<CancellationToken>()).Returns(flat);

        Func<Task> act = async () => await CreateHandler()
            .Handle(new DeactivateFlatCommand(flat.Id.Value), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
