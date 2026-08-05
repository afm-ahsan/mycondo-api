using Mediator;
using MyCondo.Application.Features.Security.Parcels.DTOs;

namespace MyCondo.Application.Features.Security.Parcels.Commands.CloseParcel;

/// <summary>Outcome must be one of Returned/Rejected/LostOrEscalated — consolidates what would
/// otherwise be three near-identical commands (mirrors SetDomesticWorkerStatusCommand's pattern).</summary>
public sealed record CloseParcelCommand(Guid ParcelId, string Outcome, string Reason) : IRequest<ParcelDto>;
