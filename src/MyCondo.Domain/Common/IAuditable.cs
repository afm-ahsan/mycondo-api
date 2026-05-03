namespace MyCondo.Domain.Common;

public interface IAuditable
{
    DateTimeOffset CreatedAtUtc { get; set; }
    Guid? CreatedBy { get; set; }
    DateTimeOffset? UpdatedAtUtc { get; set; }
    Guid? UpdatedBy { get; set; }
}
