namespace MyCondo.Domain.Features.Security.Common;

/// <summary>Entry-eligibility state shared by DomesticWorkerProfile and ServiceProviderProfile.</summary>
public enum RecurringAccessProfileStatus
{
    Active = 0,
    Suspended = 1,
    Blocked = 2,
}
