using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Utilities.Common;
using MyCondo.Application.Features.Utilities.DTOs;

namespace MyCondo.Application.Features.Utilities.Queries.GetMeterAssignmentHistory;

public sealed record GetMeterAssignmentHistoryQuery(Guid MeterId)
    : IRequest<IReadOnlyList<MeterAssignmentDto>>, IHasMeterId, IRequiresResolvedFeature;
