namespace MyCondo.Domain.Common;

public interface ISoftDeletable
{
    DateTimeOffset? DeletedAtUtc { get; set; }
    Guid? DeletedBy { get; set; }
}
