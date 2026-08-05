using Mediator;
using MyCondo.Application.Features.Security.ServiceProviderAssignments.DTOs;

namespace MyCondo.Application.Features.Security.ServiceProviderAssignments.Queries.GetAssignmentsForServiceProvider;

public sealed record GetAssignmentsForServiceProviderQuery(Guid ServiceProviderProfileId) : IRequest<List<ServiceProviderAssignmentDto>>;
