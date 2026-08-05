using MyCondo.Application.Features.Security.DomesticWorkerAssignments.DTOs;
using MyCondo.Domain.Features.Security.DomesticWorkerAssignments;

namespace MyCondo.Application.Features.Security.DomesticWorkerAssignments.Mappings;

internal static class DomesticWorkerAssignmentMappings
{
    public static DomesticWorkerAssignmentDto ToDto(this DomesticWorkerAssignment assignment) => new(
        assignment.Id.Value, assignment.DomesticWorkerProfileId.Value, assignment.FlatId.Value,
        assignment.ApprovedByResident, assignment.ValidFromUtc, assignment.ValidToUtc,
        assignment.AllowedDays.ToString(), assignment.AllowedStartTime, assignment.AllowedEndTime,
        assignment.IsActive);
}
