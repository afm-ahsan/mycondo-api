using AwesomeAssertions;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Property.Gates.DTOs;
using MyCondo.Application.Features.Property.Gates.Queries.GetGatesForTenant;
using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Domain.Features.Property.Gates;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Property.Gates.Queries.GetGatesForTenant;

public class GetGatesForTenantQueryHandlerTests
{
    private static readonly DateTimeOffset NowUtc = new(2026, 8, 13, 0, 0, 0, TimeSpan.Zero);
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly BuildingId BuildingId = new(Guid.NewGuid());

    private readonly IGateRepository _gates = Substitute.For<IGateRepository>();
    private readonly ICurrentUserProvider _currentUser = Substitute.For<ICurrentUserProvider>();

    public GetGatesForTenantQueryHandlerTests()
    {
        _currentUser.TenantId.Returns(TenantId);
    }

    private GetGatesForTenantQueryHandler CreateHandler() => new(_gates, _currentUser);

    [Fact]
    public async Task Returns_Gates_Across_All_Buildings_When_No_BuildingId_Given()
    {
        Gate gate = Gate.Create(TenantId, BuildingId, "Main Gate", "MAIN", null, true, true, 1, NowUtc);
        _gates.GetAllForTenantAsync(TenantId, null, false, Arg.Any<CancellationToken>())
            .Returns([gate]);

        List<GateDto> result = await CreateHandler().Handle(
            new GetGatesForTenantQuery(null), CancellationToken.None);

        result.Should().ContainSingle(g => g.GateId == gate.Id.Value);
    }

    [Fact]
    public async Task Filters_By_BuildingId_When_Given()
    {
        _gates.GetAllForTenantAsync(TenantId, BuildingId, false, Arg.Any<CancellationToken>())
            .Returns([]);

        await CreateHandler().Handle(new GetGatesForTenantQuery(BuildingId.Value), CancellationToken.None);

        await _gates.Received(1).GetAllForTenantAsync(TenantId, BuildingId, false, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Throws_Forbidden_When_Unauthenticated()
    {
        _currentUser.TenantId.Returns((Guid?)null);

        Func<Task> act = async () => await CreateHandler().Handle(
            new GetGatesForTenantQuery(null), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }
}
