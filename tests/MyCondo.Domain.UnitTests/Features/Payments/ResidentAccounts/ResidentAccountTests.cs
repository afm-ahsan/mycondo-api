using AwesomeAssertions;
using MyCondo.Domain.Features.Payments.ResidentAccounts;
using MyCondo.Domain.Features.Property.Flats;

namespace MyCondo.Domain.UnitTests.Features.Payments.ResidentAccounts;

public class ResidentAccountTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly FlatId FlatId = FlatId.New();

    [Fact]
    public void Open_Starts_Active_With_Version_One()
    {
        ResidentAccount account = ResidentAccount.Open(TenantId, FlatId, Now);

        account.TenantId.Should().Be(TenantId);
        account.FlatId.Should().Be(FlatId);
        account.IsActive.Should().BeTrue();
        account.OpenedAtUtc.Should().Be(Now);
        account.Version.Should().Be(1);
    }

    [Fact]
    public void Open_Throws_When_TenantId_Is_Empty()
    {
        Action act = () => ResidentAccount.Open(Guid.Empty, FlatId, Now);

        act.Should().Throw<ArgumentException>();
    }
}
