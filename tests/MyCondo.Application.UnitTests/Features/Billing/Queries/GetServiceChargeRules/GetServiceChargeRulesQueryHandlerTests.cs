using AwesomeAssertions;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Billing.Queries.GetServiceChargeRules;
using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Billing.ServiceChargeRules;
using MyCondo.Domain.Features.Property.Buildings;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Billing.Queries.GetServiceChargeRules;

public class GetServiceChargeRulesQueryHandlerTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly BuildingId BuildingId = new(Guid.NewGuid());

    private readonly IServiceChargeRuleRepository _rules = Substitute.For<IServiceChargeRuleRepository>();
    private readonly ICurrentUserProvider _currentUser = Substitute.For<ICurrentUserProvider>();

    public GetServiceChargeRulesQueryHandlerTests()
    {
        _currentUser.TenantId.Returns(TenantId);
        _rules.SearchAsync(Arg.Any<Guid>(), Arg.Any<BuildingId?>(), Arg.Any<string?>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<ServiceChargeRule>([], 1, 20, 0));
    }

    private GetServiceChargeRulesQueryHandler CreateHandler() => new(_rules, _currentUser);

    [Fact]
    public async Task Passes_Null_BuildingId_When_Omitted()
    {
        await CreateHandler().Handle(new GetServiceChargeRulesQuery(null, null, 1, 20), CancellationToken.None);

        await _rules.Received(1).SearchAsync(TenantId, null, null, 1, 20, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Passes_BuildingId_When_Given()
    {
        await CreateHandler().Handle(new GetServiceChargeRulesQuery(BuildingId.Value, null, 1, 20), CancellationToken.None);

        await _rules.Received(1).SearchAsync(TenantId, BuildingId, null, 1, 20, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Throws_Forbidden_When_Unauthenticated()
    {
        _currentUser.TenantId.Returns((Guid?)null);

        Func<Task> act = async () => await CreateHandler().Handle(
            new GetServiceChargeRulesQuery(null, null, 1, 20), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }
}
