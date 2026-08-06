using Microsoft.EntityFrameworkCore;
using MyCondo.Domain.Features.Billing.InvoiceSequences;
using MyCondo.Domain.Features.Property.Buildings;

namespace MyCondo.Infrastructure.Persistence.Repositories;

public sealed class InvoiceSequenceRepository(MyCondoDbContext db) : IInvoiceSequenceRepository
{
    /// <summary>Single atomic upsert+RETURNING statement — Postgres guarantees this is safe under
    /// concurrency for a given (tenant, building, year) row without any application-level locking.
    /// Must run inside the same transaction as the invoice insert it belongs to (see
    /// <see cref="MyCondo.Domain.Abstractions.IUnitOfWork.BeginTransactionAsync"/>) so a later
    /// failure in that same unit of work rolls the bump back too.</summary>
    public async Task<int> GetNextValueAsync(
        Guid tenantId, BuildingId buildingId, int year, CancellationToken cancellationToken)
    {
        List<int> result = await db.Database
            .SqlQuery<int>($"""
                INSERT INTO billing.invoice_sequences (tenant_id, building_id, year, next_value)
                VALUES ({tenantId}, {buildingId.Value}, {year}, 1)
                ON CONFLICT (tenant_id, building_id, year)
                DO UPDATE SET next_value = billing.invoice_sequences.next_value + 1
                RETURNING next_value AS "Value"
                """)
            .ToListAsync(cancellationToken);

        return result[0];
    }
}
