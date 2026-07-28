using Mediator;

namespace MyCondo.Application.Features.Auth.Commands.Logout;

public sealed record LogoutCommand(string RefreshToken) : IRequest;
