using MyCondo.Application.Common;
using MyCondo.Application.Features.Security.DomesticWorkers.DTOs;
using MyCondo.Domain.Features.Security.DomesticWorkers;

namespace MyCondo.Application.Features.Security.DomesticWorkers.Mappings;

internal static class DomesticWorkerProfileMappings
{
    public static DomesticWorkerProfileDto ToDto(this DomesticWorkerProfile profile) => new(
        profile.Id.Value, profile.FullName, profile.Phone, profile.WorkerType.ToString(),
        profile.IdentityDocumentType, IdentityMasking.Mask(profile.IdentityDocumentNumber),
        profile.EmergencyContactName, profile.EmergencyContactPhone, profile.VerificationStatus.ToString(),
        profile.Status.ToString(), profile.StatusReason);
}
