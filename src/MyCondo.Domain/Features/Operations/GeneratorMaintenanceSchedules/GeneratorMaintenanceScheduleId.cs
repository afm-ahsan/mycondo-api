namespace MyCondo.Domain.Features.Operations.GeneratorMaintenanceSchedules;

public readonly record struct GeneratorMaintenanceScheduleId(Guid Value)
{
    public static GeneratorMaintenanceScheduleId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();

    public static GeneratorMaintenanceScheduleId Parse(string s) =>
        Guid.TryParse(s, out Guid g)
            ? new GeneratorMaintenanceScheduleId(g)
            : throw new FormatException($"Invalid GeneratorMaintenanceScheduleId: '{s}'");
}
