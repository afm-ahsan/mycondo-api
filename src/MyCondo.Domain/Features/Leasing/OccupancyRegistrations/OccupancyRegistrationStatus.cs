namespace MyCondo.Domain.Features.Leasing.OccupancyRegistrations;

public enum OccupancyRegistrationStatus
{
    Draft = 0,
    Submitted = 1,
    OwnerApproved = 2,
    CorrectionsRequested = 3,
    ManagementVerified = 4,
    Rejected = 5,
    Active = 6,
    MovedOut = 7,
}
