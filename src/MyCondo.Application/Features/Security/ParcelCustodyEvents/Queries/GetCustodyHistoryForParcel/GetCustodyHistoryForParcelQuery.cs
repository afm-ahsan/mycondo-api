using Mediator;
using MyCondo.Application.Features.Security.ParcelCustodyEvents.DTOs;

namespace MyCondo.Application.Features.Security.ParcelCustodyEvents.Queries.GetCustodyHistoryForParcel;

public sealed record GetCustodyHistoryForParcelQuery(Guid ParcelId) : IRequest<List<ParcelCustodyEventDto>>;
