using AwesomeAssertions;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Property.Flats.Queries.GetFlatsForTenant;
using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Domain.Features.Property.Flats;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Property.Flats.Queries.GetFlatsForTenant;

public class GetFlatsForTenantQueryHandlerTests
{
    private static readonly DateTimeOffset NowUtc = new(2026, 8, 13, 0, 0, 0, TimeSpan.Zero);
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly BuildingId BuildingId = new(Guid.NewGuid());

    private readonly IFlatRepository _flats = Substitute.For<IFlatRepository>();
    private readonly ICurrentUserProvider _currentUser = Substitute.For<ICurrentUserProvider>();

    public GetFlatsForTenantQueryHandlerTests()
    {
        _currentUser.TenantId.Returns(TenantId);
    }

    private GetFlatsForTenantQueryHandler CreateHandler() => new(_flats, _currentUser);

    [Fact]
    public async Task Returns_Flats_Across_All_Buildings_When_No_BuildingId_Given()
    {
        Flat flat = Flat.Create(TenantId, BuildingId, "A-101", 1, FlatType.Residential, NowUtc);
        _flats.SearchForTenantAsync(TenantId, null, null, 1, 20, Arg.Any<CancellationToken>())
            .Returns(new PagedResult<Flat>([flat], 1, 20, 1));

        PagedResult<Application.Features.Property.Flats.DTOs.FlatDto> result = await CreateHandler().Handle(
            new GetFlatsForTenantQuery(null, null, 1, 20), CancellationToken.None);

        result.Items.Should().ContainSingle(f => f.FlatId == flat.Id.Value);
    }

    [Fact]
    public async Task Filters_By_BuildingId_When_Given()
    {
        _flats.SearchForTenantAsync(TenantId, BuildingId, null, 1, 20, Arg.Any<CancellationToken>())
            .Returns(new PagedResult<Flat>([], 1, 20, 0));

        await CreateHandler().Handle(new GetFlatsForTenantQuery(null, BuildingId.Value, 1, 20), CancellationToken.None);

        await _flats.Received(1).SearchForTenantAsync(TenantId, BuildingId, null, 1, 20, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Throws_Forbidden_When_Unauthenticated()
    {
        _currentUser.TenantId.Returns((Guid?)null);

        Func<Task> act = async () => await CreateHandler().Handle(
            new GetFlatsForTenantQuery(null, null, 1, 20), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }
}
