using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Leasing.DTOs;

namespace MyCondo.Application.Features.Leasing.Commands.VerifyOccupancyRegistrationByManagement;

public sealed record VerifyOccupancyRegistrationByManagementCommand(Guid OccupancyRegistrationId) : IRequest<OccupancyRegistrationDto>, ILifecycleWriteOperation;
