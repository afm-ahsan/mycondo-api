using System.Security.Claims;
using AwesomeAssertions;
using Microsoft.AspNetCore.Http;
using MyCondo.Api.Authentication;

namespace MyCondo.Api.IntegrationTests;

/// <summary>
/// Pure claims-level tests for <see cref="CurrentUserProvider.HasPermissionForBuilding"/> — no host,
/// no DB. Cross-tenant isolation (case 3 of ADR-014's required matrix) isn't exercised here: a JWT
/// only ever carries one tenant's claims (see ADR-013), so "another tenant's building" is simply a
/// building ID that never appears in this user's `bperm` claims at all — the same code path as
/// "different building, same tenant" below. Genuine cross-tenant proof lives in the RLS/multi-tenancy
/// tests, not here.
/// </summary>
public class CurrentUserProviderTests
{
    private static readonly Guid BuildingA = Guid.Parse("00000000-0000-0000-0000-00000000000a");
    private static readonly Guid BuildingB = Guid.Parse("00000000-0000-0000-0000-00000000000b");
    private static readonly Guid BuildingC = Guid.Parse("00000000-0000-0000-0000-00000000000c");
    private const string Permission = "complaint.manage";

    private static CurrentUserProvider CreateProvider(IEnumerable<Claim> claims)
    {
        ClaimsIdentity identity = new(claims, "TestAuth");
        DefaultHttpContext httpContext = new() { User = new ClaimsPrincipal(identity) };
        FakeHttpContextAccessor accessor = new(httpContext);
        return new CurrentUserProvider(accessor);
    }

    [Fact]
    public void Permitted_Building_Grants_Access()
    {
        CurrentUserProvider provider = CreateProvider([new Claim("bperm", $"{BuildingA}|{Permission}")]);

        provider.HasPermissionForBuilding(Permission, BuildingA).Should().BeTrue();
    }

    [Fact]
    public void Different_Building_Same_Tenant_Is_Denied()
    {
        CurrentUserProvider provider = CreateProvider([new Claim("bperm", $"{BuildingA}|{Permission}")]);

        provider.HasPermissionForBuilding(Permission, BuildingB).Should().BeFalse();
    }

    [Fact]
    public void Tenant_Wide_Grant_Applies_To_Any_Building()
    {
        CurrentUserProvider provider = CreateProvider([new Claim("perm", Permission)]);

        provider.HasPermissionForBuilding(Permission, BuildingA).Should().BeTrue();
        provider.HasPermissionForBuilding(Permission, BuildingB).Should().BeTrue();
        provider.HasPermissionForBuilding(Permission, buildingId: null).Should().BeTrue();
    }

    [Fact]
    public void Multiple_Building_Assignments_Each_Grant_Only_Their_Own_Building()
    {
        CurrentUserProvider provider = CreateProvider(
        [
            new Claim("bperm", $"{BuildingA}|{Permission}"),
            new Claim("bperm", $"{BuildingB}|{Permission}"),
        ]);

        provider.HasPermissionForBuilding(Permission, BuildingA).Should().BeTrue();
        provider.HasPermissionForBuilding(Permission, BuildingB).Should().BeTrue();
        provider.HasPermissionForBuilding(Permission, BuildingC).Should().BeFalse();
    }

    [Fact]
    public void No_Assignment_Is_Denied()
    {
        CurrentUserProvider provider = CreateProvider([]);

        provider.HasPermissionForBuilding(Permission, BuildingA).Should().BeFalse();
        provider.HasPermissionForBuilding(Permission, buildingId: null).Should().BeFalse();
    }

    [Fact]
    public void Building_Grant_Does_Not_Leak_To_A_Non_Building_Scoped_Request()
    {
        // A user who only holds a building-scoped grant must not pass a check for the same
        // permission with no building context (buildingId: null) — that would let a building-scoped
        // grant masquerade as tenant-wide.
        CurrentUserProvider provider = CreateProvider([new Claim("bperm", $"{BuildingA}|{Permission}")]);

        provider.HasPermissionForBuilding(Permission, buildingId: null).Should().BeFalse();
    }

    private sealed class FakeHttpContextAccessor(HttpContext httpContext) : IHttpContextAccessor
    {
        public HttpContext? HttpContext { get; set; } = httpContext;
    }
}
