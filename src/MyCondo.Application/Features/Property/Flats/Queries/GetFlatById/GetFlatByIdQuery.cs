using Mediator;
using MyCondo.Application.Features.Property.Flats.DTOs;

namespace MyCondo.Application.Features.Property.Flats.Queries.GetFlatById;

public sealed record GetFlatByIdQuery(Guid FlatId) : IRequest<FlatDto>;
