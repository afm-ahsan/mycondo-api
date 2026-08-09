using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Authorization;
using MyCondo.Domain.Features.Identity.Permissions;

namespace MyCondo.Application.Common.Services;

public sealed class PermissionSeeder(
    IPermissionRepository permissions,
    ILogger<PermissionSeeder> logger
) : IPermissionSeeder
{
    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        List<Permission> existing = await permissions.GetAllAsync(cancellationToken);
        HashSet<string> existingNames = existing.Select(p => p.Name).ToHashSet(StringComparer.Ordinal);

        int inserted = 0;
        foreach ((string name, string description, string module, bool isBuildingScopable) in PermissionCatalogue.Entries)
        {
            if (existingNames.Contains(name))
            {
                continue;
            }

            permissions.Add(Permission.Create(PermissionId.New(), name, description, module, isBuildingScopable));
            inserted++;
        }

        logger.LogInformation(
            "[DatabaseSeed] Permissions: {Expected} expected, {Inserted} inserted, {Existing} existing",
            PermissionCatalogue.Entries.Length, inserted, existingNames.Count);
    }
}
