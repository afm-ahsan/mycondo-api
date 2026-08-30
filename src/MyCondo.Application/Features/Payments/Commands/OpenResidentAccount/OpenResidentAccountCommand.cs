using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Payments.DTOs;

namespace MyCondo.Application.Features.Payments.Commands.OpenResidentAccount;

public sealed record OpenResidentAccountCommand(Guid FlatId) : IRequest<ResidentAccountDto>, ILifecycleWriteOperation;
