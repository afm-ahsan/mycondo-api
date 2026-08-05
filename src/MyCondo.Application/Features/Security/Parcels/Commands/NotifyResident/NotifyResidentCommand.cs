using Mediator;
using MyCondo.Application.Features.Security.Parcels.DTOs;

namespace MyCondo.Application.Features.Security.Parcels.Commands.NotifyResident;

public sealed record NotifyResidentCommand(Guid ParcelId) : IRequest<ParcelDto>;
