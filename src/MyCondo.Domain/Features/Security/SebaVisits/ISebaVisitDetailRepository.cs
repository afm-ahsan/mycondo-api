using MyCondo.Domain.Features.Security.AccessSessions;

namespace MyCondo.Domain.Features.Security.SebaVisits;

public interface ISebaVisitDetailRepository
{
    Task<SebaVisitDetail?> GetByAccessSessionIdAsync(AccessSessionId accessSessionId, CancellationToken cancellationToken);

    void Add(SebaVisitDetail detail);
}
