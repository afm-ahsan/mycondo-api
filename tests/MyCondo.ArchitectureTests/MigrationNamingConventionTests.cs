using System.Reflection;
using System.Text.RegularExpressions;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore.Migrations;

namespace MyCondo.ArchitectureTests;

/// <summary>
/// Enforces the EF Core migration naming standard documented in
/// docs/conventions/03-database/02-ef-core-and-migrations.md: a 14-digit timestamp prefix followed by
/// an underscore and a strict-PascalCase identifier in the form &lt;Verb&gt;&lt;Subject&gt;&lt;Purpose&gt;,
/// with no vague placeholder names. Reads the [Migration] attribute directly so it catches drift between
/// the file name and the recorded id too.
/// </summary>
public class MigrationNamingConventionTests
{
    private static readonly Regex MigrationIdPattern = new(
        @"^\d{14}_[A-Z][A-Za-z0-9]*$", RegexOptions.Compiled);

    private static readonly string[] BannedNames =
    [
        "Changes",
        "SchemaChanges",
        "Migration1",
        "UpdateDatabase",
        "LatestMigration",
        "FinalFix",
        "FixMigration",
    ];

    private static IEnumerable<(string TypeName, string MigrationId)> GetMigrations() =>
        typeof(Infrastructure.Persistence.MyCondoDbContext).Assembly
            .GetTypes()
            .Where(t => typeof(Migration).IsAssignableFrom(t) && !t.IsAbstract)
            .Select(t => (t.Name, MigrationId: t.GetCustomAttribute<MigrationAttribute>()?.Id
                ?? throw new InvalidOperationException($"{t.Name} is missing a [Migration] attribute.")));

    [Fact]
    public void Migration_Ids_Should_Match_Timestamp_And_PascalCase_Convention()
    {
        List<string> offenders = GetMigrations()
            .Where(m => !MigrationIdPattern.IsMatch(m.MigrationId))
            .Select(m => m.MigrationId)
            .ToList();

        offenders.Should().BeEmpty(
            "every migration id must be `<14-digit timestamp>_<PascalCase identifier>` " +
            "with no underscores inside the identifier " +
            "(see docs/conventions/03-database/02-ef-core-and-migrations.md)");
    }

    [Fact]
    public void Migration_Ids_Should_Not_Use_Banned_Vague_Names()
    {
        List<string> offenders = GetMigrations()
            .Select(m => m.MigrationId)
            .Where(id => BannedNames.Any(banned =>
                id.EndsWith("_" + banned, StringComparison.Ordinal)))
            .ToList();

        offenders.Should().BeEmpty(
            $"migration names must describe schema/business intent, not one of: {string.Join(", ", BannedNames)}");
    }

    [Fact]
    public void Migration_Class_Name_Should_Match_The_Recorded_Migration_Id()
    {
        List<string> offenders = GetMigrations()
            .Where(m => m.MigrationId != $"{DateTimeStamp(m.MigrationId)}_{m.TypeName}")
            .Select(m => $"{m.TypeName} vs [Migration(\"{m.MigrationId}\")]")
            .ToList();

        offenders.Should().BeEmpty(
            "the migration class name must match the identifier portion of its [Migration] attribute");
    }

    private static string DateTimeStamp(string migrationId) =>
        migrationId.Length >= 14 ? migrationId[..14] : migrationId;
}
