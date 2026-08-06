using AwesomeAssertions;
using MyCondo.Domain.Features.Payments.Idempotency;
using MyCondo.Domain.Features.Payments.Idempotency.Exceptions;

namespace MyCondo.Domain.UnitTests.Features.Payments.Idempotency;

public class IdempotencyKeyTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;
    private static readonly Guid TenantId = Guid.NewGuid();

    private static IdempotencyKey RecordKey(string requestHash = "hash-1") =>
        IdempotencyKey.Record(TenantId, "client-key-1", "/api/v1/payments", requestHash, 200, "{}", Now);

    [Fact]
    public void Record_Sets_Expected_Fields()
    {
        IdempotencyKey key = RecordKey();

        key.TenantId.Should().Be(TenantId);
        key.Key.Should().Be("client-key-1");
        key.RequestPath.Should().Be("/api/v1/payments");
        key.ResponseStatusCode.Should().Be(200);
    }

    [Fact]
    public void EnsureMatches_Does_Not_Throw_For_Same_Hash()
    {
        IdempotencyKey key = RecordKey("hash-1");

        Action act = () => key.EnsureMatches("hash-1");

        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureMatches_Throws_For_Different_Hash()
    {
        IdempotencyKey key = RecordKey("hash-1");

        Action act = () => key.EnsureMatches("hash-2");

        act.Should().Throw<IdempotencyKeyConflictException>();
    }
}
