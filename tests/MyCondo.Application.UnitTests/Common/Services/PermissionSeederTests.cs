using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Authorization;
using MyCondo.Application.Common.Services;
using MyCondo.Domain.Features.Identity.Permissions;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Common.Services;

public class PermissionSeederTests
{
    private static (IPermissionRepository Permissions, ILogger<PermissionSeeder> Logger, List<Permission> Added)
        BuildSubstitute(IEnumerable<Permission> existing)
    {
        IPermissionRepository permissions = Substitute.For<IPermissionRepository>();
        ILogger<PermissionSeeder> logger = Substitute.For<ILogger<PermissionSeeder>>();
        permissions.GetAllAsync(Arg.Any<CancellationToken>()).Returns(existing.ToList());

        List<Permission> added = [];
        permissions.Add(Arg.Do<Permission>(p => added.Add(p)));

        return (permissions, logger, added);
    }

    [Fact]
    public async Task SeedAsync_On_Empty_Database_Inserts_Every_Catalogue_Entry()
    {
        (IPermissionRepository permissions, ILogger<PermissionSeeder> logger, List<Permission> added) =
            BuildSubstitute([]);

        PermissionSeeder seeder = new(permissions, logger);
        await seeder.SeedAsync(CancellationToken.None);

        added.Select(p => p.Name).Should().BeEquivalentTo(PermissionCatalogue.Entries.Select(e => e.Name));
    }

    [Fact]
    public async Task SeedAsync_When_Catalogue_Already_Fully_Present_Inserts_Nothing()
    {
        List<Permission> existing = PermissionCatalogue.Entries
            .Select(e => Permission.Create(PermissionId.New(), e.Name, e.Description, e.Module, e.IsBuildingScopable))
            .ToList();

        (IPermissionRepository permissions, ILogger<PermissionSeeder> logger, List<Permission> added) =
            BuildSubstitute(existing);

        PermissionSeeder seeder = new(permissions, logger);
        await seeder.SeedAsync(CancellationToken.None);

        added.Should().BeEmpty();
    }

    [Fact]
    public async Task SeedAsync_Only_Inserts_Entries_Missing_From_The_Database()
    {
        Permission[] existing =
        [
            Permission.Create(PermissionId.New(), "tenant.view", "View tenant details", "tenant", false),
            Permission.Create(PermissionId.New(), "user.view", "View users", "user", false),
        ];

        (IPermissionRepository permissions, ILogger<PermissionSeeder> logger, List<Permission> added) =
            BuildSubstitute(existing);

        PermissionSeeder seeder = new(permissions, logger);
        await seeder.SeedAsync(CancellationToken.None);

        added.Should().HaveCount(PermissionCatalogue.Entries.Length - 2);
        added.Select(p => p.Name).Should().NotContain(["tenant.view", "user.view"]);
    }

    [Fact]
    public async Task SeedAsync_Never_Calls_Remove_Or_Update_For_An_Unrecognized_Existing_Permission()
    {
        Permission handInserted = Permission.Create(PermissionId.New(), "custom.made-up", "desc", "custom", false);
        (IPermissionRepository permissions, ILogger<PermissionSeeder> logger, List<Permission> added) =
            BuildSubstitute([handInserted]);

        PermissionSeeder seeder = new(permissions, logger);
        await seeder.SeedAsync(CancellationToken.None);

        // IPermissionRepository has no Remove/Update member at all (additive-only by construction —
        // see its doc comment) — this test's real assertion is that PermissionSeeder still inserts
        // every catalogue entry despite the unrecognized row's presence, i.e. it never short-circuits.
        added.Select(p => p.Name).Should().BeEquivalentTo(PermissionCatalogue.Entries.Select(e => e.Name));
    }
}
