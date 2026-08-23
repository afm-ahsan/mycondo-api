using AwesomeAssertions;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Common.Services;
using MyCondo.Application.Features.Security.Parcels.Queries.GetParcelsForTenant;
using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Domain.Features.Property.Flats;
using MyCondo.Domain.Features.Security.Parcels;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Security.Parcels.Queries.GetParcelsForTenant;

public class GetParcelsForTenantQueryHandlerTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly BuildingId BuildingId = new(Guid.NewGuid());

    private readonly IParcelRepository _parcels = Substitute.For<IParcelRepository>();
    private readonly IFlatDisplayNameResolver _flatDisplayNames = Substitute.For<IFlatDisplayNameResolver>();
    private readonly ICurrentUserProvider _currentUser = Substitute.For<ICurrentUserProvider>();

    public GetParcelsForTenantQueryHandlerTests()
    {
        _currentUser.TenantId.Returns(TenantId);
        _parcels.SearchAsync(
                Arg.Any<Guid>(), Arg.Any<ParcelStatus?>(), Arg.Any<FlatId?>(), Arg.Any<BuildingId?>(),
                Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<Parcel>([], 1, 20, 0));
    }

    private GetParcelsForTenantQueryHandler CreateHandler() => new(_parcels, _flatDisplayNames, _currentUser);

    [Fact]
    public async Task Passes_Null_BuildingId_When_Omitted()
    {
        await CreateHandler().Handle(new GetParcelsForTenantQuery(null, null, 1, 20), CancellationToken.None);

        await _parcels.Received(1).SearchAsync(TenantId, null, null, null, 1, 20, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Passes_BuildingId_When_Given()
    {
        await CreateHandler().Handle(
            new GetParcelsForTenantQuery(null, null, 1, 20, BuildingId.Value), CancellationToken.None);

        await _parcels.Received(1).SearchAsync(TenantId, null, null, BuildingId, 1, 20, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Throws_Forbidden_When_Unauthenticated()
    {
        _currentUser.TenantId.Returns((Guid?)null);

        Func<Task> act = async () => await CreateHandler().Handle(
            new GetParcelsForTenantQuery(null, null, 1, 20), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }
}
