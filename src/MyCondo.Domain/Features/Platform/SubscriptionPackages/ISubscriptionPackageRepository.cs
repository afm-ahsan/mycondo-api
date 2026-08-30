namespace MyCondo.Domain.Features.Platform.SubscriptionPackages;

public interface ISubscriptionPackageRepository
{
    Task<List<SubscriptionPackage>> GetAllAsync(CancellationToken cancellationToken);

    void Add(SubscriptionPackage package);
}
