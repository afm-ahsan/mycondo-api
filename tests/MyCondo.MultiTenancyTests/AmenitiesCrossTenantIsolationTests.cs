using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using MyCondo.Domain.Features.Amenities.Bookings;
using MyCondo.Domain.Features.Amenities.Facilities;
using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Domain.Features.Property.Flats;
using MyCondo.Infrastructure.Persistence;

namespace MyCondo.MultiTenancyTests;

/// <summary>
/// Real RLS enforcement tests for the Slice G `amenities` schema tables (facilities, blackout_dates,
/// bookings, pool_sessions, pool_incidents), plus a behavioral proof of the booking-overlap
/// `ex_bookings_no_overlap` EXCLUDE/GiST constraint — same pattern as
/// <see cref="UtilitiesCrossTenantIsolationTests"/>. Requires a Docker daemon. Written and reviewed
/// for correctness but NOT executed in the environment they were authored in — run wherever Docker is
/// actually available before trusting them.
/// </summary>
public class AmenitiesCrossTenantIsolationTests : IClassFixture<MultiTenancyPostgresFixture>
{
    private readonly MultiTenancyPostgresFixture _fixture;

    public AmenitiesCrossTenantIsolationTests(MultiTenancyPostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Facilities_Cross_Tenant_Isolation()
    {
        Guid tenantA = Guid.NewGuid();
        Guid tenantB = Guid.NewGuid();
        BuildingId buildingA = BuildingId.New();
        BuildingId buildingB = BuildingId.New();

        await using (MyCondoDbContext dbA = _fixture.CreateDbContext(tenantA))
        {
            dbA.Set<Facility>().Add(Facility.Create(
                tenantA, buildingA, "Hall A", FacilityType.CommunityHall, 100, null, null, true, 500m, 2000m, 24, 50m,
                null, null, false, false, DateTimeOffset.UtcNow));
            await dbA.SaveChangesAsync();
        }

        await using (MyCondoDbContext dbB = _fixture.CreateDbContext(tenantB))
        {
            dbB.Set<Facility>().Add(Facility.Create(
                tenantB, buildingB, "Hall B", FacilityType.CommunityHall, 100, null, null, true, 500m, 2000m, 24, 50m,
                null, null, false, false, DateTimeOffset.UtcNow));
            await dbB.SaveChangesAsync();
        }

        await using (MyCondoDbContext asTenantA = _fixture.CreateDbContext(tenantA))
        {
            List<Facility> visible = await asTenantA.Set<Facility>().ToListAsync();
            visible.Should().ContainSingle(f => f.Name == "Hall A");
            visible.Should().NotContain(f => f.Name == "Hall B");
        }
    }

    [Fact]
    public async Task Insert_Facility_For_Wrong_Tenant_Is_Rejected_By_Rls()
    {
        Guid tenantA = Guid.NewGuid();
        Guid tenantB = Guid.NewGuid();

        await using MyCondoDbContext dbAsTenantB = _fixture.CreateDbContext(tenantB);

        dbAsTenantB.Set<Facility>().Add(Facility.Create(
            tenantA, BuildingId.New(), "Impersonator Hall", FacilityType.CommunityHall, 50, null, null, false, null,
            null, 24, 0m, null, null, false, false, DateTimeOffset.UtcNow));

        Func<Task> act = () => dbAsTenantB.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task All_Slice_G_Amenities_Tables_Have_Rls_Enabled_And_Forced()
    {
        (string Schema, string Table)[] tables =
        [
            ("amenities", "facilities"),
            ("amenities", "blackout_dates"),
            ("amenities", "bookings"),
            ("amenities", "pool_sessions"),
            ("amenities", "pool_incidents"),
        ];

        await using MyCondoDbContext db = _fixture.CreateDbContext(tenantId: null);

        foreach ((string schema, string table) in tables)
        {
            RlsFlags row = await db.Database
                .SqlQuery<RlsFlags>(
                    $"""
                    SELECT relrowsecurity AS row_security, relforcerowsecurity AS force_row_security
                    FROM pg_class
                    WHERE oid = ({schema + "."} || {table})::regclass
                    """)
                .SingleAsync();

            row.RowSecurity.Should().BeTrue($"{schema}.{table} must have RLS enabled");
            row.ForceRowSecurity.Should().BeTrue($"{schema}.{table} must FORCE RLS (the migrator role owns the table)");
        }
    }

    [Fact]
    public async Task Overlapping_Confirmed_Booking_For_Same_Facility_Is_Rejected_By_Exclusion_Constraint()
    {
        Guid tenantId = Guid.NewGuid();
        BuildingId buildingId = BuildingId.New();
        FacilityId facilityId = FacilityId.New();
        FlatId flatId = FlatId.New();
        DateTimeOffset start = DateTimeOffset.UtcNow.AddDays(10);

        await using MyCondoDbContext db = _fixture.CreateDbContext(tenantId);

        db.Set<Facility>().Add(Facility.Create(
            tenantId, buildingId, "Hall", FacilityType.CommunityHall, 100, null, null, false, 500m, 0m, 24, 0m, null,
            null, false, false, DateTimeOffset.UtcNow));

        Booking first = Booking.Request(
            tenantId, facilityId, buildingId, flatId, "First event", start, start.AddHours(3), 30, 30, 20, false, 0m,
            0m, 24, 0m, null, DateTimeOffset.UtcNow);
        db.Set<Booking>().Add(first);
        await db.SaveChangesAsync();

        Booking overlapping = Booking.Request(
            tenantId, facilityId, buildingId, flatId, "Overlapping event", start.AddHours(1), start.AddHours(4), 30, 30,
            20, false, 0m, 0m, 24, 0m, null, DateTimeOffset.UtcNow);
        db.Set<Booking>().Add(overlapping);

        Func<Task> act = () => db.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    private sealed class RlsFlags
    {
        public bool RowSecurity { get; init; }
        public bool ForceRowSecurity { get; init; }
    }
}
