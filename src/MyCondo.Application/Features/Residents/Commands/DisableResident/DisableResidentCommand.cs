using Mediator;
using MyCondo.Application.Common.Abstractions;

namespace MyCondo.Application.Features.Residents.Commands.DisableResident;

public sealed record DisableResidentCommand(Guid ResidentId) : IRequest, ILifecycleWriteOperation;
