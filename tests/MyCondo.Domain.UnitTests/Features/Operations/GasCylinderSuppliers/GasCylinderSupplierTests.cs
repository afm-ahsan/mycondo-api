using AwesomeAssertions;
using MyCondo.Domain.Features.Operations.GasCylinderSuppliers;

namespace MyCondo.Domain.UnitTests.Features.Operations.GasCylinderSuppliers;

public class GasCylinderSupplierTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;
    private static readonly Guid TenantId = Guid.NewGuid();

    [Fact]
    public void Create_Starts_Active()
    {
        GasCylinderSupplier supplier = GasCylinderSupplier.Create(TenantId, "Padma Oil", "01712345678", null, "Dhaka", Now);

        supplier.IsActive.Should().BeTrue();
        supplier.Version.Should().Be(1);
    }

    [Fact]
    public void Create_Throws_When_Name_Empty()
    {
        Action act = () => GasCylinderSupplier.Create(TenantId, "", null, null, null, Now);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Deactivate_Then_Reactivate_Toggles_IsActive()
    {
        GasCylinderSupplier supplier = GasCylinderSupplier.Create(TenantId, "Padma Oil", null, null, null, Now);

        supplier.Deactivate();
        supplier.IsActive.Should().BeFalse();

        supplier.Reactivate();
        supplier.IsActive.Should().BeTrue();
        supplier.Version.Should().Be(3);
    }
}
