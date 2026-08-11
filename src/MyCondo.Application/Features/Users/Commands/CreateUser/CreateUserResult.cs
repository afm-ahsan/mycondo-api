namespace MyCondo.Application.Features.Users.Commands.CreateUser;

/// <summary>
/// <see cref="TemporaryPassword"/> is populated only when the caller did not supply
/// <c>InitialPassword</c> — it is never persisted or retrievable again after this response.
/// </summary>
public sealed record CreateUserResult(Guid UserId, string? TemporaryPassword);
