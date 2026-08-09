using AwesomeAssertions;
using MyCondo.Domain.Features.Property.FlatOwnerships;
using MyCondo.Domain.Features.Property.Flats;

namespace MyCondo.Domain.UnitTests.Features.Property.FlatOwnerships;

public class FlatOwnershipTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    [Fact]
    public void Grant_Creates_An_Active_Ownership()
    {
        Guid tenantId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();
        FlatId flatId = FlatId.New();
        DateOnly startDate = DateOnly.FromDateTime(DateTime.UtcNow);

        FlatOwnership ownership = FlatOwnership.Grant(tenantId, userId, flatId, startDate, Now);

        ownership.TenantId.Should().Be(tenantId);
        ownership.UserId.Should().Be(userId);
        ownership.FlatId.Should().Be(flatId);
        ownership.StartDate.Should().Be(startDate);
        ownership.EndDate.Should().BeNull();
        ownership.Status.Should().Be(FlatOwnershipStatus.Active);
    }

    [Fact]
    public void Grant_Throws_When_TenantId_Is_Empty()
    {
        Action act = () => FlatOwnership.Grant(
            Guid.Empty, Guid.NewGuid(), FlatId.New(), DateOnly.FromDateTime(DateTime.UtcNow), Now);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Grant_Throws_When_UserId_Is_Empty()
    {
        Action act = () => FlatOwnership.Grant(
            Guid.NewGuid(), Guid.Empty, FlatId.New(), DateOnly.FromDateTime(DateTime.UtcNow), Now);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void End_Sets_Status_To_Ended_And_Records_The_End_Date()
    {
        FlatOwnership ownership = FlatOwnership.Grant(
            Guid.NewGuid(), Guid.NewGuid(), FlatId.New(), DateOnly.FromDateTime(DateTime.UtcNow), Now);
        DateOnly endDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));

        ownership.End(endDate, Now.AddDays(1));

        ownership.Status.Should().Be(FlatOwnershipStatus.Ended);
        ownership.EndDate.Should().Be(endDate);
    }

    [Fact]
    public void End_Is_Idempotent_When_Already_Ended()
    {
        FlatOwnership ownership = FlatOwnership.Grant(
            Guid.NewGuid(), Guid.NewGuid(), FlatId.New(), DateOnly.FromDateTime(DateTime.UtcNow), Now);
        DateOnly firstEndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
        ownership.End(firstEndDate, Now.AddDays(1));

        ownership.End(DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)), Now.AddDays(5));

        ownership.EndDate.Should().Be(firstEndDate, "a second End() call must not overwrite the original end date");
    }
}
