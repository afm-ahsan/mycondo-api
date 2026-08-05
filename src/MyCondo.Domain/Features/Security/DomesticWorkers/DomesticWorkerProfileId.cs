namespace MyCondo.Domain.Features.Security.DomesticWorkers;

public readonly record struct DomesticWorkerProfileId(Guid Value)
{
    public static DomesticWorkerProfileId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();

    public static DomesticWorkerProfileId Parse(string s) =>
        Guid.TryParse(s, out Guid g)
            ? new DomesticWorkerProfileId(g)
            : throw new FormatException($"Invalid DomesticWorkerProfileId: '{s}'");
}
