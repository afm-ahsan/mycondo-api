using Mediator;
using MyCondo.Application.Features.Utilities.DTOs;

namespace MyCondo.Application.Features.Utilities.Commands.ReviewReading;

public sealed record ReviewReadingCommand(Guid ReadingId) : IRequest<ReadingDto>;
