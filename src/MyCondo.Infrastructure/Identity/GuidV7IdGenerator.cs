using MyCondo.Domain.Abstractions;

namespace MyCondo.Infrastructure.Identity;

public sealed class GuidV7IdGenerator : IIdGenerator
{
    public Guid NewUuidV7() => Guid.CreateVersion7();
}
