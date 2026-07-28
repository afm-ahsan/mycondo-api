using System.Security.Cryptography;
using System.Text;
using Konscious.Security.Cryptography;
using Microsoft.Extensions.Options;
using MyCondo.Application.Common.Abstractions;

namespace MyCondo.Infrastructure.Identity;

/// <summary>
/// Argon2id password hashing per OWASP recommendations as of 2026.
/// Encoded format: <c>$argon2id$v=19$m=&lt;memory&gt;,t=&lt;iterations&gt;,p=&lt;parallelism&gt;$&lt;salt-b64&gt;$&lt;hash-b64&gt;</c>.
/// Cost parameters tunable via <see cref="Argon2Settings"/> — bump as hardware improves.
/// </summary>
public sealed class Argon2idPasswordHasher(IOptions<Argon2Settings> options) : IPasswordHasher
{
    private const int SaltBytes = 16;
    private const int HashBytes = 32;

    private readonly Argon2Settings _settings = options.Value;

    public string Hash(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        byte[] salt = RandomNumberGenerator.GetBytes(SaltBytes);
        byte[] hash = ComputeHash(password, salt, _settings);

        return $"$argon2id$v=19$m={_settings.MemoryKb},t={_settings.Iterations},p={_settings.Parallelism}"
             + $"${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    public bool Verify(string password, string encodedHash)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(encodedHash))
        {
            return false;
        }

        Argon2Settings? parsed = TryParse(encodedHash, out byte[]? salt, out byte[]? expected);
        if (parsed is null || salt is null || expected is null)
        {
            return false;
        }

        byte[] actual = ComputeHash(password, salt, parsed);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }

    private static byte[] ComputeHash(string password, byte[] salt, Argon2Settings settings)
    {
        using Argon2id argon = new(Encoding.UTF8.GetBytes(password))
        {
            Salt = salt,
            DegreeOfParallelism = settings.Parallelism,
            MemorySize = settings.MemoryKb,
            Iterations = settings.Iterations,
        };
        return argon.GetBytes(HashBytes);
    }

    private static Argon2Settings? TryParse(string encoded, out byte[]? salt, out byte[]? hash)
    {
        salt = null;
        hash = null;

        if (!encoded.StartsWith("$argon2id$", StringComparison.Ordinal))
        {
            return null;
        }

        string[] parts = encoded.Split('$', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 5)
        {
            return null;
        }
        // parts: ["argon2id", "v=19", "m=...,t=...,p=...", "<salt-b64>", "<hash-b64>"]

        Dictionary<string, int> kv = parts[2]
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Split('=', 2))
            .Where(p => p.Length == 2 && int.TryParse(p[1], out _))
            .ToDictionary(p => p[0], p => int.Parse(p[1], System.Globalization.CultureInfo.InvariantCulture));

        if (!kv.TryGetValue("m", out int m) ||
            !kv.TryGetValue("t", out int t) ||
            !kv.TryGetValue("p", out int p))
        {
            return null;
        }

        try
        {
            salt = Convert.FromBase64String(parts[3]);
            hash = Convert.FromBase64String(parts[4]);
        }
        catch (FormatException)
        {
            return null;
        }

        return new Argon2Settings { MemoryKb = m, Iterations = t, Parallelism = p };
    }
}
