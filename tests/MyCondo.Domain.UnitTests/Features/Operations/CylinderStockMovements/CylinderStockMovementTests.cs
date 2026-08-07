using AwesomeAssertions;
using MyCondo.Domain.Features.Operations.CylinderStockMovements;

namespace MyCondo.Domain.UnitTests.Features.Operations.CylinderStockMovements;

public class CylinderStockMovementTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;
    private static readonly Guid TenantId = Guid.NewGuid();

    [Fact]
    public void Receive_Stores_Positive_Quantity()
    {
        CylinderStockMovement movement = CylinderStockMovement.Receive(TenantId, "LPG-12kg", 20, Now, Guid.NewGuid(), null, Now);

        movement.MovementType.Should().Be(CylinderStockMovementType.Receipt);
        movement.Quantity.Should().Be(20);
    }

    [Fact]
    public void Issue_Stores_Negative_Quantity()
    {
        CylinderStockMovement movement = CylinderStockMovement.Issue(TenantId, "LPG-12kg", 5, Now, Guid.NewGuid(), Now);

        movement.MovementType.Should().Be(CylinderStockMovementType.Issue);
        movement.Quantity.Should().Be(-5);
    }

    [Fact]
    public void ReturnEmpty_Stores_Negative_Quantity()
    {
        CylinderStockMovement movement = CylinderStockMovement.ReturnEmpty(TenantId, "LPG-12kg", 3, Now, Guid.NewGuid(), Now);

        movement.MovementType.Should().Be(CylinderStockMovementType.EmptyReturn);
        movement.Quantity.Should().Be(-3);
    }

    [Fact]
    public void Receive_Throws_When_Quantity_Not_Positive()
    {
        Action act = () => CylinderStockMovement.Receive(TenantId, "LPG-12kg", 0, Now, null, null, Now);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Adjust_Allows_Negative_Signed_Quantity_With_Reason()
    {
        CylinderStockMovement movement = CylinderStockMovement.Adjust(TenantId, "LPG-12kg", -2, "Physical count correction", Now, Guid.NewGuid(), Now);

        movement.MovementType.Should().Be(CylinderStockMovementType.Adjustment);
        movement.Quantity.Should().Be(-2);
        movement.Reason.Should().Be("Physical count correction");
    }

    [Fact]
    public void Adjust_Throws_When_Quantity_Is_Zero()
    {
        Action act = () => CylinderStockMovement.Adjust(TenantId, "LPG-12kg", 0, "Reason", Now, null, Now);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Adjust_Throws_When_Reason_Empty()
    {
        Action act = () => CylinderStockMovement.Adjust(TenantId, "LPG-12kg", 1, "", Now, null, Now);

        act.Should().Throw<ArgumentException>();
    }
}
