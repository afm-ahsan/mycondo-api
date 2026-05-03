namespace MyCondo.Domain.Common;

public interface ITenantScoped
{
    Guid TenantId { get; }
}
