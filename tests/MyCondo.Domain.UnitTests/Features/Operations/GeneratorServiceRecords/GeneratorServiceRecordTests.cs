using AwesomeAssertions;
using MyCondo.Domain.Features.Operations.Generators;
using MyCondo.Domain.Features.Operations.GeneratorServiceRecords;

namespace MyCondo.Domain.UnitTests.Features.Operations.GeneratorServiceRecords;

public class GeneratorServiceRecordTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly GeneratorId GeneratorId = GeneratorId.New();

    [Fact]
    public void Record_Succeeds_With_Description()
    {
        GeneratorServiceRecord record = GeneratorServiceRecord.Record(
            TenantId, GeneratorId, Now, "Replaced oil filter", 800m, Guid.NewGuid(), Now);

        record.Description.Should().Be("Replaced oil filter");
        record.Cost.Should().Be(800m);
    }

    [Fact]
    public void Record_Throws_When_Description_Empty()
    {
        Action act = () => GeneratorServiceRecord.Record(TenantId, GeneratorId, Now, "", null, null, Now);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Record_Throws_When_Cost_Negative()
    {
        Action act = () => GeneratorServiceRecord.Record(TenantId, GeneratorId, Now, "Service", -1m, null, Now);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
