using MyCondo.Application.Features.Security.ServiceProviderAssignments.DTOs;
using MyCondo.Domain.Features.Security.ServiceProviderAssignments;

namespace MyCondo.Application.Features.Security.ServiceProviderAssignments.Mappings;

internal static class ServiceProviderAssignmentMappings
{
    public static ServiceProviderAssignmentDto ToDto(this ServiceProviderAssignment assignment) => new(
        assignment.Id.Value, assignment.ServiceProviderProfileId.Value, assignment.FlatId.Value,
        assignment.ApprovedByResident, assignment.ValidFromUtc, assignment.ValidToUtc,
        assignment.AllowedDays.ToString(), assignment.AllowedStartTime, assignment.AllowedEndTime,
        assignment.IsActive);
}
