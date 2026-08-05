using Mediator;
using MyCondo.Application.Features.Security.Guests.DTOs;

namespace MyCondo.Application.Features.Security.Guests.Commands.CreateGuestProfile;

public sealed record CreateGuestProfileCommand(
    string FullName,
    string Phone,
    string? IdentityDocumentType,
    string? IdentityDocumentNumber
) : IRequest<GuestProfileDto>;
