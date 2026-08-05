using Mediator;
using MyCondo.Application.Features.Residents.DTOs;

namespace MyCondo.Application.Features.Residents.Queries.GetResidentById;

public sealed record GetResidentByIdQuery(Guid ResidentId) : IRequest<ResidentDto>;
