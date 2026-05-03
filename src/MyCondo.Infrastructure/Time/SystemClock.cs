using MyCondo.Domain.Abstractions;

namespace MyCondo.Infrastructure.Time;

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
