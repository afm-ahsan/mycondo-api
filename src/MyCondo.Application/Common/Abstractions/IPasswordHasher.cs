namespace MyCondo.Application.Common.Abstractions;

public interface IPasswordHasher
{
    /// <summary>Hashes a plaintext password and returns an encoded hash string.</summary>
    string Hash(string password);

    /// <summary>Verifies a plaintext password against an encoded hash.</summary>
    bool Verify(string password, string encodedHash);
}
