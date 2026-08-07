using Mediator;
using MyCondo.Application.Features.Operations.DTOs;

namespace MyCondo.Application.Features.Operations.Queries.GetOpenGeneratorSession;

public sealed record GetOpenGeneratorSessionQuery(Guid GeneratorId) : IRequest<GeneratorSessionDto?>;
