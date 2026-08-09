using Mediator;

namespace MyCondo.Application.Features.Platform.Commands.RevokePlatformToken;

public sealed record RevokePlatformTokenCommand(string RefreshToken) : IRequest;
