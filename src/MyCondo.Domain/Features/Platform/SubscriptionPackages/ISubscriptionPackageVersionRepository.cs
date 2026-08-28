namespace MyCondo.Domain.Features.Platform.SubscriptionPackages;

public interface ISubscriptionPackageVersionRepository
{
    Task<List<SubscriptionPackageVersion>> GetAllAsync(CancellationToken cancellationToken);

    void Add(SubscriptionPackageVersion version);
}
