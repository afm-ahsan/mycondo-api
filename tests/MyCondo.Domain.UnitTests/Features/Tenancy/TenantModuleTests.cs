using AwesomeAssertions;
using MyCondo.Domain.Features.Tenancy;

namespace MyCondo.Domain.UnitTests.Features.Tenancy;

public class TenantModuleTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    [Fact]
    public void Enable_Sets_All_Fields()
    {
        Guid tenantId = Guid.NewGuid();
        Guid enabledBy = Guid.NewGuid();

        TenantModule module = TenantModule.Enable(tenantId, "billing", Now, enabledBy);

        module.TenantId.Should().Be(tenantId);
        module.ModuleKey.Should().Be("billing");
        module.EnabledAtUtc.Should().Be(Now);
        module.EnabledBy.Should().Be(enabledBy);
    }

    [Fact]
    public void Enable_Throws_For_Unknown_Module_Key()
    {
        Action act = () => TenantModule.Enable(Guid.NewGuid(), "not-a-real-module", Now, null);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void TenantModuleKeys_Excludes_Always_On_Foundation_Modules()
    {
        TenantModuleKeys.All.Should().NotContain("tenancy");
        TenantModuleKeys.All.Should().NotContain("identity");
    }
}
