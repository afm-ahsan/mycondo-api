using AwesomeAssertions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Tenancy.Queries.GetTenantBySlug;
using MyCondo.Domain.Features.Tenancy;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Tenancy.Queries.GetTenantBySlug;

/// <summary>
/// Tenant sign-in's "Organization" field is resolved through this handler. Users identify their
/// organization by display name (e.g. "Akter Residence Park"), not by the internal slug (e.g.
/// "arp"), so a slug miss must fall back to a case-insensitive exact match on the tenant's name
/// before giving up.
/// </summary>
public class GetTenantBySlugQueryHandlerTests
{
    private static readonly DateTimeOffset NowUtc = new(2026, 8, 10, 0, 0, 0, TimeSpan.Zero);

    private readonly ITenantRepository _tenants = Substitute.For<ITenantRepository>();

    private GetTenantBySlugQueryHandler CreateHandler() => new(_tenants);

    [Fact]
    public async Task Resolves_By_Exact_Slug_Match()
    {
        Tenant tenant = Tenant.Provision("Akter Residence Park", "arp", NowUtc);
        _tenants.GetBySlugAsync("arp", Arg.Any<CancellationToken>()).Returns(tenant);

        TenantSummaryDto result =
            await CreateHandler().Handle(new GetTenantBySlugQuery("arp"), CancellationToken.None);

        result.Slug.Should().Be("arp");
        result.Name.Should().Be("Akter Residence Park");
        await _tenants.DidNotReceive().GetByNameAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Falls_Back_To_Case_Insensitive_Name_Match_When_Slug_Lookup_Misses()
    {
        Tenant tenant = Tenant.Provision("Akter Residence Park", "arp", NowUtc);
        _tenants.GetBySlugAsync("akter residence park", Arg.Any<CancellationToken>())
            .Returns((Tenant?)null);
        _tenants.GetByNameAsync("Akter Residence Park", Arg.Any<CancellationToken>())
            .Returns(tenant);

        TenantSummaryDto result =
            await CreateHandler().Handle(
                new GetTenantBySlugQuery("Akter Residence Park"), CancellationToken.None);

        result.Slug.Should().Be("arp");
        result.Name.Should().Be("Akter Residence Park");
    }

    [Fact]
    public async Task Trims_Whitespace_Before_Resolving()
    {
        Tenant tenant = Tenant.Provision("Akter Residence Park", "arp", NowUtc);
        _tenants.GetBySlugAsync("arp", Arg.Any<CancellationToken>()).Returns(tenant);

        await CreateHandler().Handle(new GetTenantBySlugQuery("  arp  "), CancellationToken.None);

        await _tenants.Received(1).GetBySlugAsync("arp", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Throws_NotFound_When_Neither_Slug_Nor_Name_Match()
    {
        _tenants.GetBySlugAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((Tenant?)null);
        _tenants.GetByNameAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((Tenant?)null);

        Func<Task> act = async () =>
            await CreateHandler().Handle(new GetTenantBySlugQuery("no-such-org"), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
