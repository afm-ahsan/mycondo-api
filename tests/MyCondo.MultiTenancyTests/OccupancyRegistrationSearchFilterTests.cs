using AwesomeAssertions;
using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Leasing.OccupancyRegistrations;
using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Domain.Features.Property.Flats;
using MyCondo.Domain.Features.Residents;
using MyCondo.Infrastructure.Persistence;
using MyCondo.Infrastructure.Persistence.Repositories;

namespace MyCondo.MultiTenancyTests;

/// <summary>
/// Real Postgres proofs for `OccupancyRegistrationRepository.SearchAsync`'s <c>search</c> filter (name,
/// email, phone, and — via a <c>Flat</c> join — flat number), added because a mocked repository test
/// only proves the handler passes the term through, not that the actual EF LINQ-to-SQL WHERE clause
/// matches correctly. Same pattern as <see cref="BookingSearchFilterTests"/>. Requires a Docker daemon.
/// </summary>
public class OccupancyRegistrationSearchFilterTests : IClassFixture<MultiTenancyPostgresFixture>
{
    private readonly MultiTenancyPostgresFixture _fixture;

    public OccupancyRegistrationSearchFilterTests(MultiTenancyPostgresFixture fixture)
    {
        _fixture = fixture;
    }

    private static OccupancyRegistration MakeRegistration(
        Guid tenantId, FlatId flatId, string fullName, string? phone, string? email,
        OccupancyRegistrationStatus status = OccupancyRegistrationStatus.Draft) =>
        OccupancyRegistration.Register(
            tenantId, flatId, ResidentId.New(), ResidentType.Occupant, fullName, phone, email, null, null, null,
            null, null, null, null, null, null, null, null, DateTimeOffset.UtcNow);

    private static async Task<FlatId> SeedFlatAsync(MyCondoDbContext db, Guid tenantId, string flatNumber)
    {
        Building building = Building.Create(tenantId, "Tower A", "TA", null, DateTimeOffset.UtcNow);
        Flat flat = Flat.Create(tenantId, building.Id, flatNumber, null, FlatType.Residential, DateTimeOffset.UtcNow);
        db.Set<Building>().Add(building);
        db.Set<Flat>().Add(flat);
        await db.SaveChangesAsync();
        return flat.Id;
    }

    [Fact]
    public async Task Search_Matches_Primary_Full_Name_Case_Insensitively()
    {
        Guid tenantId = Guid.NewGuid();
        await using MyCondoDbContext db = _fixture.CreateDbContext(tenantId);
        FlatId flatId = await SeedFlatAsync(db, tenantId, "A-101");

        db.Set<OccupancyRegistration>().Add(MakeRegistration(tenantId, flatId, "Karim Ahmed", null, null));
        db.Set<OccupancyRegistration>().Add(MakeRegistration(tenantId, flatId, "Nusrat Jahan", null, null));
        await db.SaveChangesAsync();

        OccupancyRegistrationRepository repository = new(db);
        PagedResult<OccupancyRegistration> result = await repository.SearchAsync(
            tenantId, null, null, "karim", 1, 20, CancellationToken.None);

        result.Items.Should().ContainSingle(r => r.PrimaryFullName == "Karim Ahmed");
    }

    [Fact]
    public async Task Search_Matches_Primary_Email_As_A_Partial_Match()
    {
        Guid tenantId = Guid.NewGuid();
        await using MyCondoDbContext db = _fixture.CreateDbContext(tenantId);
        FlatId flatId = await SeedFlatAsync(db, tenantId, "A-101");

        db.Set<OccupancyRegistration>().Add(MakeRegistration(tenantId, flatId, "Karim Ahmed", null, "karim@example.com"));
        db.Set<OccupancyRegistration>().Add(MakeRegistration(tenantId, flatId, "Nusrat Jahan", null, "nusrat@example.com"));
        await db.SaveChangesAsync();

        OccupancyRegistrationRepository repository = new(db);
        PagedResult<OccupancyRegistration> result = await repository.SearchAsync(
            tenantId, null, null, "nusrat@", 1, 20, CancellationToken.None);

        result.Items.Should().ContainSingle(r => r.PrimaryFullName == "Nusrat Jahan");
    }

    [Fact]
    public async Task Search_Matches_Primary_Phone_As_A_Partial_Match()
    {
        Guid tenantId = Guid.NewGuid();
        await using MyCondoDbContext db = _fixture.CreateDbContext(tenantId);
        FlatId flatId = await SeedFlatAsync(db, tenantId, "A-101");

        db.Set<OccupancyRegistration>().Add(MakeRegistration(tenantId, flatId, "Karim Ahmed", "01700000001", null));
        db.Set<OccupancyRegistration>().Add(MakeRegistration(tenantId, flatId, "Nusrat Jahan", "01700000002", null));
        await db.SaveChangesAsync();

        OccupancyRegistrationRepository repository = new(db);
        PagedResult<OccupancyRegistration> result = await repository.SearchAsync(
            tenantId, null, null, "0002", 1, 20, CancellationToken.None);

        result.Items.Should().ContainSingle(r => r.PrimaryFullName == "Nusrat Jahan");
    }

    [Fact]
    public async Task Search_Matches_Flat_Number_Via_The_Flat_Join()
    {
        Guid tenantId = Guid.NewGuid();
        await using MyCondoDbContext db = _fixture.CreateDbContext(tenantId);
        FlatId flatA = await SeedFlatAsync(db, tenantId, "A-101");
        FlatId flatB = await SeedFlatAsync(db, tenantId, "B-202");

        db.Set<OccupancyRegistration>().Add(MakeRegistration(tenantId, flatA, "Karim Ahmed", null, null));
        db.Set<OccupancyRegistration>().Add(MakeRegistration(tenantId, flatB, "Nusrat Jahan", null, null));
        await db.SaveChangesAsync();

        OccupancyRegistrationRepository repository = new(db);
        PagedResult<OccupancyRegistration> result = await repository.SearchAsync(
            tenantId, null, null, "b-202", 1, 20, CancellationToken.None);

        result.Items.Should().ContainSingle(r => r.PrimaryFullName == "Nusrat Jahan");
    }

    [Fact]
    public async Task Search_Combines_With_The_Status_Filter()
    {
        Guid tenantId = Guid.NewGuid();
        await using MyCondoDbContext db = _fixture.CreateDbContext(tenantId);
        FlatId flatId = await SeedFlatAsync(db, tenantId, "A-101");

        OccupancyRegistration draft = MakeRegistration(tenantId, flatId, "Karim Ahmed", null, null);
        OccupancyRegistration submitted = MakeRegistration(tenantId, flatId, "Karim Ahmed", null, null);
        submitted.UpdateDraft("Karim Ahmed", null, null, "1234567890123", DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-30)), "Male", null, null, null, null, null, null, null, null);
        submitted.Submit(null, DateTimeOffset.UtcNow);
        db.Set<OccupancyRegistration>().AddRange(draft, submitted);
        await db.SaveChangesAsync();

        OccupancyRegistrationRepository repository = new(db);
        PagedResult<OccupancyRegistration> result = await repository.SearchAsync(
            tenantId, null, OccupancyRegistrationStatus.Submitted, "karim", 1, 20, CancellationToken.None);

        result.Total.Should().Be(1);
        result.Items.Should().ContainSingle().Which.Status.Should().Be(OccupancyRegistrationStatus.Submitted);
    }

    [Fact]
    public async Task Search_Never_Returns_Registrations_From_Another_Tenant()
    {
        Guid tenantA = Guid.NewGuid();
        Guid tenantB = Guid.NewGuid();

        await using (MyCondoDbContext dbA = _fixture.CreateDbContext(tenantA))
        {
            FlatId flatA = await SeedFlatAsync(dbA, tenantA, "A-101");
            dbA.Set<OccupancyRegistration>().Add(MakeRegistration(tenantA, flatA, "Shared Name", null, null));
            await dbA.SaveChangesAsync();
        }

        await using (MyCondoDbContext dbB = _fixture.CreateDbContext(tenantB))
        {
            FlatId flatB = await SeedFlatAsync(dbB, tenantB, "A-101");
            dbB.Set<OccupancyRegistration>().Add(MakeRegistration(tenantB, flatB, "Shared Name", null, null));
            await dbB.SaveChangesAsync();
        }

        await using MyCondoDbContext asTenantA = _fixture.CreateDbContext(tenantA);
        OccupancyRegistrationRepository repository = new(asTenantA);
        PagedResult<OccupancyRegistration> result = await repository.SearchAsync(
            tenantA, null, null, "shared", 1, 20, CancellationToken.None);

        result.Total.Should().Be(1);
        result.Items.Should().ContainSingle().Which.TenantId.Should().Be(tenantA);
    }
}
