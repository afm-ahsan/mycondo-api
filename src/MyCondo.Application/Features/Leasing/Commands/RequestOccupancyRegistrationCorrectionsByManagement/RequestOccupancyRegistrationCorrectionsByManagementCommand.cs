using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Leasing.DTOs;

namespace MyCondo.Application.Features.Leasing.Commands.RequestOccupancyRegistrationCorrectionsByManagement;

public sealed record RequestOccupancyRegistrationCorrectionsByManagementCommand(
    Guid OccupancyRegistrationId, string Reason
) : IRequest<OccupancyRegistrationDto>, ILifecycleWriteOperation;
