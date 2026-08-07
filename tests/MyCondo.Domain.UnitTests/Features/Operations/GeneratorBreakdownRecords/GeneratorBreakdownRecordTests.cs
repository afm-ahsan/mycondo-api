using AwesomeAssertions;
using MyCondo.Domain.Features.Operations.GeneratorBreakdownRecords;
using MyCondo.Domain.Features.Operations.GeneratorBreakdownRecords.Exceptions;
using MyCondo.Domain.Features.Operations.Generators;

namespace MyCondo.Domain.UnitTests.Features.Operations.GeneratorBreakdownRecords;

public class GeneratorBreakdownRecordTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly GeneratorId GeneratorId = GeneratorId.New();

    [Fact]
    public void Report_Creates_Unresolved_Record()
    {
        GeneratorBreakdownRecord record = GeneratorBreakdownRecord.Report(
            TenantId, GeneratorId, Now, "Engine overheating", Now, Now);

        record.DowntimeEndUtc.Should().BeNull();
        record.Resolution.Should().BeNull();
    }

    [Fact]
    public void Resolve_Sets_Resolution_And_DowntimeEnd()
    {
        GeneratorBreakdownRecord record = GeneratorBreakdownRecord.Report(
            TenantId, GeneratorId, Now, "Engine overheating", Now, Now);

        record.Resolve("Replaced coolant pump", 1500m, Now.AddHours(3));

        record.Resolution.Should().Be("Replaced coolant pump");
        record.Cost.Should().Be(1500m);
        record.DowntimeEndUtc.Should().Be(Now.AddHours(3));
    }

    [Fact]
    public void Resolve_Throws_When_Already_Resolved()
    {
        GeneratorBreakdownRecord record = GeneratorBreakdownRecord.Report(
            TenantId, GeneratorId, Now, "Engine overheating", Now, Now);
        record.Resolve("Replaced coolant pump", 1500m, Now.AddHours(3));

        Action act = () => record.Resolve("Second fix", null, Now.AddHours(5));

        act.Should().Throw<GeneratorBreakdownAlreadyResolvedException>();
    }

    [Fact]
    public void Resolve_Throws_When_DowntimeEnd_Precedes_DowntimeStart()
    {
        GeneratorBreakdownRecord record = GeneratorBreakdownRecord.Report(
            TenantId, GeneratorId, Now, "Engine overheating", Now, Now);

        Action act = () => record.Resolve("Replaced coolant pump", null, Now.AddHours(-1));

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
