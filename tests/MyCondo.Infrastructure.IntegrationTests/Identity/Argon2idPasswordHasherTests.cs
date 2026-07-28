using AwesomeAssertions;
using Microsoft.Extensions.Options;
using MyCondo.Infrastructure.Identity;

namespace MyCondo.Infrastructure.IntegrationTests.Identity;

public class Argon2idPasswordHasherTests
{
    // Low cost factors on purpose — this test verifies correctness, not production tuning
    // (see mycondo-api/docs/kickoff.md open question on pinning real Argon2id cost parameters).
    private readonly Argon2idPasswordHasher _hasher = new(
        Options.Create(new Argon2Settings { MemoryKb = 8192, Iterations = 1, Parallelism = 1 }));

    [Fact]
    public void Hash_Then_Verify_With_Correct_Password_Succeeds()
    {
        string hash = _hasher.Hash("correct-horse-battery-staple");

        _hasher.Verify("correct-horse-battery-staple", hash).Should().BeTrue();
    }

    [Fact]
    public void Verify_With_Wrong_Password_Fails()
    {
        string hash = _hasher.Hash("correct-horse-battery-staple");

        _hasher.Verify("wrong-password", hash).Should().BeFalse();
    }

    [Fact]
    public void Hash_Is_Salted_Differently_Each_Time()
    {
        string hash1 = _hasher.Hash("same-password");
        string hash2 = _hasher.Hash("same-password");

        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void Verify_Returns_False_For_Malformed_Hash()
    {
        _hasher.Verify("anything", "not-a-real-hash").Should().BeFalse();
    }

    [Fact]
    public void Verify_Returns_False_For_Empty_Password_Or_Hash()
    {
        _hasher.Verify("", "$argon2id$whatever").Should().BeFalse();
        _hasher.Verify("password", "").Should().BeFalse();
    }
}
