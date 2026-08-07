using AwesomeAssertions;
using MyCondo.Domain.Features.Operations.GeneratorFuelReceipts;
using MyCondo.Domain.Features.Operations.Generators;

namespace MyCondo.Domain.UnitTests.Features.Operations.GeneratorFuelReceipts;

public class GeneratorFuelReceiptTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly GeneratorId GeneratorId = GeneratorId.New();

    [Fact]
    public void Record_Succeeds_With_Positive_Quantity()
    {
        GeneratorFuelReceipt receipt = GeneratorFuelReceipt.Record(
            TenantId, GeneratorId, Now, 50m, 5000m, "Padma Oil", "Monthly top-up", Now);

        receipt.Quantity.Should().Be(50m);
        receipt.Cost.Should().Be(5000m);
    }

    [Fact]
    public void Record_Throws_When_Quantity_Not_Positive()
    {
        Action act = () => GeneratorFuelReceipt.Record(TenantId, GeneratorId, Now, 0m, null, null, null, Now);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Record_Throws_When_Cost_Negative()
    {
        Action act = () => GeneratorFuelReceipt.Record(TenantId, GeneratorId, Now, 10m, -1m, null, null, Now);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
