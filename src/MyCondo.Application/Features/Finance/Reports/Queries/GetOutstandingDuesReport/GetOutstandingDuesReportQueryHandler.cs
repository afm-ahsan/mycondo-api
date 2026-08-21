using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Finance.Reports.Contracts;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Billing.Invoices;
using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Domain.Features.Property.Flats;

namespace MyCondo.Application.Features.Finance.Reports.Queries.GetOutstandingDuesReport;

/// <summary>Simple per-flat outstanding total — reuses the same open-invoice population as the
/// existing Receivable Ageing report (<c>IInvoiceRepository</c>'s Issued/PartiallyPaid/PartiallyWaived
/// statuses), pre-aggregated per Flat server-side rather than age-bucketed.</summary>
public sealed class GetOutstandingDuesReportQueryHandler(
    IInvoiceRepository invoices,
    IFlatRepository flats,
    ICurrentUserProvider currentUser,
    IClock clock
) : IRequestHandler<GetOutstandingDuesReportQuery, OutstandingDuesReportDto>
{
    public async ValueTask<OutstandingDuesReportDto> Handle(GetOutstandingDuesReportQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        BuildingId? buildingId = query.BuildingId is Guid rawBuildingId ? new BuildingId(rawBuildingId) : null;

        IReadOnlyList<FlatReceivableLine> receivables = await invoices.GetOpenReceivablesByFlatAsync(
            tenantId, buildingId, cancellationToken);

        List<Flat> flatEntities = await flats.GetByIdsAsync(
            receivables.Select(r => r.FlatId).ToList(), cancellationToken);
        Dictionary<FlatId, Flat> flatsById = flatEntities.ToDictionary(f => f.Id);

        List<OutstandingDuesLineDto> lines = receivables
            .Where(r => r.Balance != 0m)
            .Select(r =>
            {
                flatsById.TryGetValue(r.FlatId, out Flat? flat);
                return new OutstandingDuesLineDto(
                    r.FlatId.Value, flat?.FlatNumber ?? "Unknown", flat?.BuildingId.Value ?? Guid.Empty,
                    r.Balance, r.OpenInvoiceCount);
            })
            .OrderByDescending(l => l.OutstandingBalance)
            .ToList();

        DateOnly asOfDate = DateOnly.FromDateTime(clock.UtcNow.UtcDateTime);
        string scopeSummary = buildingId is null ? "Tenant (all buildings)" : $"Building {buildingId.Value}";
        FinanceReportMetadataDto metadata = FinanceReportMetadataDto.ForAsOf(
            asOfDate, scopeSummary, clock.UtcNow, currentUser.UserId);

        return new OutstandingDuesReportDto(metadata, lines, lines.Sum(l => l.OutstandingBalance));
    }
}
