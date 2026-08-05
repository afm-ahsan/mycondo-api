using Microsoft.EntityFrameworkCore;
using MyCondo.Domain.Features.Security.AccessSessions;
using MyCondo.Domain.Features.Security.SebaVisits;

namespace MyCondo.Infrastructure.Persistence.Repositories;

public sealed class SebaVisitDetailRepository(MyCondoDbContext db) : ISebaVisitDetailRepository
{
    public Task<SebaVisitDetail?> GetByAccessSessionIdAsync(AccessSessionId accessSessionId, CancellationToken cancellationToken) =>
        db.Set<SebaVisitDetail>().FirstOrDefaultAsync(d => d.AccessSessionId == accessSessionId, cancellationToken);

    public void Add(SebaVisitDetail detail) => db.Set<SebaVisitDetail>().Add(detail);
}
