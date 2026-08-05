namespace MyCondo.Domain.Features.Security.AccessSessions;

/// <summary>
/// Discriminates which profile an <see cref="AccessSession"/> belongs to. Only <see cref="Guest"/> and
/// <see cref="Vehicle"/> are wired to a profile entity and command/query handlers as of Slice B —
/// DomesticWorker/ServiceProvider/Staff/SebaVisitor are declared now (so this shared table's
/// discriminator doesn't need a disruptive migration later) but have no profile entity or handler yet;
/// they are Slice B2+ scope.
/// </summary>
public enum AccessCategory
{
    Guest = 0,
    Vehicle = 1,
    DomesticWorker = 2,
    ServiceProvider = 3,
    Staff = 4,
    SebaVisitor = 5,
}
