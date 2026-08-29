using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Leasing.DTOs;

namespace MyCondo.Application.Features.Leasing.Commands.RejectOccupancyRegistrationByManagement;

public sealed record RejectOccupancyRegistrationByManagementCommand(
    Guid OccupancyRegistrationId, string Reason
) : IRequest<OccupancyRegistrationDto>, ILifecycleWriteOperation;
