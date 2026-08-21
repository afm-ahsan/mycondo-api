using MyCondo.Domain.Features.Billing.Invoices;
using MyCondo.Domain.Features.Property.Flats;

namespace MyCondo.Application.Features.Billing.Services;

public interface IResponsiblePartyResolver
{
    /// <summary>Resolves the flat's current responsible party as of <paramref name="asOfDate"/> for
    /// snapshotting onto a new <see cref="Invoice"/> — the flat's active OccupancyRegistration tenant
    /// if one exists, otherwise its active FlatOwnership owner, otherwise <c>null</c>. See
    /// <see cref="ResponsiblePartySnapshot"/>'s doc comment for the full policy.</summary>
    Task<ResponsiblePartySnapshot?> ResolveAsync(
        Guid tenantId, FlatId flatId, DateOnly asOfDate, CancellationToken cancellationToken);
}
