using AwesomeAssertions;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Property.Flats.DTOs;
using MyCondo.Application.Features.Property.Flats.Queries.GetFlatById;
using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Domain.Features.Property.Flats;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Property.Flats.Queries.GetFlatById;

public class GetFlatByIdQueryHandlerTests
{
    private static readonly DateTimeOffset NowUtc = new(2026, 8, 11, 0, 0, 0, TimeSpan.Zero);
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid OtherTenantId = Guid.NewGuid();
    private static readonly BuildingId TestBuildingId = BuildingId.New();

    private readonly IFlatRepository _flats = Substitute.For<IFlatRepository>();
    private readonly ICurrentUserProvider _currentUser = Substitute.For<ICurrentUserProvider>();

    public GetFlatByIdQueryHandlerTests()
    {
        _currentUser.TenantId.Returns(TenantId);
    }

    private GetFlatByIdQueryHandler CreateHandler() => new(_flats, _currentUser);

    [Fact]
    public async Task Returns_The_Flat_When_It_Belongs_To_The_Callers_Tenant()
    {
        Flat flat = Flat.Create(TenantId, TestBuildingId, "A-101", 1, FlatType.Residential, NowUtc);
        _flats.GetByIdAsync(flat.Id, Arg.Any<CancellationToken>()).Returns(flat);

        FlatDto result = await CreateHandler().Handle(new GetFlatByIdQuery(flat.Id.Value), CancellationToken.None);

        result.FlatNumber.Should().Be("A-101");
        result.BuildingId.Should().Be(TestBuildingId.Value);
    }

    [Fact]
    public async Task Throws_NotFound_When_Flat_Belongs_To_A_Different_Tenant()
    {
        Flat flat = Flat.Create(OtherTenantId, TestBuildingId, "A-101", 1, FlatType.Residential, NowUtc);
        _flats.GetByIdAsync(flat.Id, Arg.Any<CancellationToken>()).Returns(flat);

        Func<Task> act = async () => await CreateHandler().Handle(new GetFlatByIdQuery(flat.Id.Value), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Throws_NotFound_When_Flat_Does_Not_Exist()
    {
        Guid missingId = Guid.NewGuid();
        _flats.GetByIdAsync(new FlatId(missingId), Arg.Any<CancellationToken>()).Returns((Flat?)null);

        Func<Task> act = async () => await CreateHandler().Handle(new GetFlatByIdQuery(missingId), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
