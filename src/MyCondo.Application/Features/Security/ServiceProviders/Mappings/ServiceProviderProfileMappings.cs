using MyCondo.Application.Common;
using MyCondo.Application.Features.Security.ServiceProviders.DTOs;
using MyCondo.Domain.Features.Security.ServiceProviders;

namespace MyCondo.Application.Features.Security.ServiceProviders.Mappings;

internal static class ServiceProviderProfileMappings
{
    public static ServiceProviderProfileDto ToDto(this ServiceProviderProfile profile) => new(
        profile.Id.Value, profile.FullName, profile.Phone, profile.ProviderType.ToString(),
        profile.ServiceDescription, profile.IdentityDocumentType, IdentityMasking.Mask(profile.IdentityDocumentNumber),
        profile.VerificationStatus.ToString(), profile.Status.ToString(), profile.StatusReason);
}
