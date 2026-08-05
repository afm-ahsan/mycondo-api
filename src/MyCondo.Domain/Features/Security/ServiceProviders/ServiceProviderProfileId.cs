namespace MyCondo.Domain.Features.Security.ServiceProviders;

public readonly record struct ServiceProviderProfileId(Guid Value)
{
    public static ServiceProviderProfileId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();

    public static ServiceProviderProfileId Parse(string s) =>
        Guid.TryParse(s, out Guid g)
            ? new ServiceProviderProfileId(g)
            : throw new FormatException($"Invalid ServiceProviderProfileId: '{s}'");
}
