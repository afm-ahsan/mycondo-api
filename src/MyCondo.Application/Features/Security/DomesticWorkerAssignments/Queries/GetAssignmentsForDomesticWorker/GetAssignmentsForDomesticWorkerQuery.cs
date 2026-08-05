using Mediator;
using MyCondo.Application.Features.Security.DomesticWorkerAssignments.DTOs;

namespace MyCondo.Application.Features.Security.DomesticWorkerAssignments.Queries.GetAssignmentsForDomesticWorker;

public sealed record GetAssignmentsForDomesticWorkerQuery(Guid DomesticWorkerProfileId) : IRequest<List<DomesticWorkerAssignmentDto>>;
