using Mediator;
using MyCondo.Application.Common.Abstractions;

namespace MyCondo.Application.Features.Operations.Queries.ExportGeneratorOperationalReport;

public sealed record ExportGeneratorOperationalReportQuery(
    Guid? GeneratorId,
    DateOnly FromDate,
    DateOnly ToDate,
    ReportExportFormat Format
) : IRequest<ReportExportResult>, IRequiresFeature
{
    public string FeatureKey => "operations.generator";
}
