using AwesomeAssertions;
using MyCondo.Domain.Features.Leasing.HouseholdMembers;
using MyCondo.Domain.Features.Leasing.OccupancyRegistrations;

namespace MyCondo.Domain.UnitTests.Features.Leasing.HouseholdMembers;

public class HouseholdMemberTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly OccupancyRegistrationId RegistrationId = OccupancyRegistrationId.New();

    [Fact]
    public void Add_Starts_Active()
    {
        HouseholdMember member = HouseholdMember.Add(
            TenantId, RegistrationId, "John Doe", "Spouse", new DateOnly(1992, 5, 1), "01711111111", null, Now);

        member.IsActive.Should().BeTrue();
        member.FullName.Should().Be("John Doe");
        member.RelationshipToPrimary.Should().Be("Spouse");
    }

    [Fact]
    public void Deactivate_Sets_IsActive_False()
    {
        HouseholdMember member = HouseholdMember.Add(TenantId, RegistrationId, "John Doe", "Spouse", null, null, null, Now);

        member.Deactivate();

        member.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Add_Throws_When_FullName_Empty()
    {
        Action act = () => HouseholdMember.Add(TenantId, RegistrationId, "", "Spouse", null, null, null, Now);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Add_Throws_When_TenantId_Empty()
    {
        Action act = () => HouseholdMember.Add(Guid.Empty, RegistrationId, "John Doe", "Spouse", null, null, null, Now);

        act.Should().Throw<ArgumentException>();
    }
}
