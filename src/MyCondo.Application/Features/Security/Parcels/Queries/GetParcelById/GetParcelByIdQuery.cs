using Mediator;
using MyCondo.Application.Features.Security.Parcels.DTOs;

namespace MyCondo.Application.Features.Security.Parcels.Queries.GetParcelById;

public sealed record GetParcelByIdQuery(Guid ParcelId) : IRequest<ParcelDto>;
