using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Operations.DTOs;
using MyCondo.Domain.Features.Operations.GeneratorFuelReceipts;
using MyCondo.Domain.Features.Operations.Generators;
using MyCondo.Domain.Features.Operations.GeneratorSessions;

namespace MyCondo.Application.Features.Operations.Queries.GetGeneratorOperationalReport;

public sealed class GetGeneratorOperationalReportQueryHandler(
    IGeneratorSessionRepository sessions,
    IGeneratorFuelReceiptRepository fuelReceipts,
    IGeneratorRepository generators,
    ICurrentUserProvider currentUser
) : IRequestHandler<GetGeneratorOperationalReportQuery, IReadOnlyList<GeneratorOperationalReportLineDto>>
{
    public async ValueTask<IReadOnlyList<GeneratorOperationalReportLineDto>> Handle(
        GetGeneratorOperationalReportQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        GeneratorId? generatorId = query.GeneratorId is Guid raw ? new GeneratorId(raw) : null;
        DateTimeOffset fromUtc = new(query.FromDate, TimeOnly.MinValue, TimeSpan.Zero);
        DateTimeOffset toUtc = new(query.ToDate.AddDays(1), TimeOnly.MinValue, TimeSpan.Zero);

        IReadOnlyList<GeneratorSession> periodSessions = await sessions.GetForPeriodAsync(tenantId, fromUtc, toUtc, generatorId, cancellationToken);
        IReadOnlyList<GeneratorFuelReceipt> periodReceipts = await fuelReceipts.GetForPeriodAsync(tenantId, fromUtc, toUtc, generatorId, cancellationToken);

        HashSet<GeneratorId> generatorIds = [.. periodSessions.Select(s => s.GeneratorId), .. periodReceipts.Select(r => r.GeneratorId)];

        List<GeneratorOperationalReportLineDto> lines = [];
        foreach (GeneratorId id in generatorIds)
        {
            Generator? generator = await generators.GetByIdAsync(id, cancellationToken);
            List<GeneratorSession> generatorSessions = [.. periodSessions.Where(s => s.GeneratorId == id)];
            List<GeneratorFuelReceipt> generatorReceipts = [.. periodReceipts.Where(r => r.GeneratorId == id)];

            int totalRuntimeMinutes = generatorSessions.Sum(s => s.RuntimeMinutes ?? 0);
            decimal totalFuelConsumed = generatorSessions.Sum(s => s.OpeningFuelLevel - (s.ClosingFuelLevel ?? s.OpeningFuelLevel));
            decimal totalFuelReceived = generatorReceipts.Sum(r => r.Quantity);
            decimal totalFuelCost = generatorReceipts.Sum(r => r.Cost ?? 0m);

            lines.Add(new GeneratorOperationalReportLineDto(
                id.Value, generator?.Name ?? "(unknown)", generatorSessions.Count, totalRuntimeMinutes, totalFuelConsumed,
                totalFuelReceived, totalFuelCost));
        }

        return lines;
    }
}
