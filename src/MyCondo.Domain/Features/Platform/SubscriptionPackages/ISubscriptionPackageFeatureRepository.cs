namespace MyCondo.Domain.Features.Platform.SubscriptionPackages;

public interface ISubscriptionPackageFeatureRepository
{
    Task<List<SubscriptionPackageFeature>> GetAllAsync(CancellationToken cancellationToken);

    void Add(SubscriptionPackageFeature packageFeature);
}
