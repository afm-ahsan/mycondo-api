using System.ComponentModel.DataAnnotations;

namespace MyCondo.Infrastructure.Identity;

public sealed record Argon2Settings
{
    public const string SectionName = "Argon2";

    /// <summary>Memory cost in KiB. OWASP recommendation: ≥ 19 MiB (19456). Tune for deployment.</summary>
    [Range(8192, 1048576)]
    public int MemoryKb { get; init; } = 19_456;

    /// <summary>Time cost (iterations). OWASP recommendation: ≥ 2.</summary>
    [Range(1, 16)]
    public int Iterations { get; init; } = 2;

    /// <summary>Parallelism (lanes). Match deployment vCPU count.</summary>
    [Range(1, 64)]
    public int Parallelism { get; init; } = 1;
}
