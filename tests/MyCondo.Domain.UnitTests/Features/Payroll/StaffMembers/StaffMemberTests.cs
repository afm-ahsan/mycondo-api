using AwesomeAssertions;
using MyCondo.Domain.Features.Payroll.StaffMembers;

namespace MyCondo.Domain.UnitTests.Features.Payroll.StaffMembers;

public class StaffMemberTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;
    private static readonly Guid TenantId = Guid.NewGuid();

    [Fact]
    public void Register_Trims_FullName_And_Starts_Active()
    {
        StaffMember staffMember = StaffMember.Register(TenantId, "  John Guard  ", StaffRole.Guard, "017", Now);

        staffMember.FullName.Should().Be("John Guard");
        staffMember.IsActive.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Register_Throws_When_FullName_Is_Blank(string fullName)
    {
        Action act = () => StaffMember.Register(TenantId, fullName, StaffRole.Guard, null, Now);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Deactivate_Sets_IsActive_False_And_DeletedAtUtc()
    {
        StaffMember staffMember = StaffMember.Register(TenantId, "John Guard", StaffRole.Guard, null, Now);

        staffMember.Deactivate(Now.AddDays(1), Guid.NewGuid());

        staffMember.IsActive.Should().BeFalse();
        staffMember.DeletedAtUtc.Should().Be(Now.AddDays(1));
    }
}
