using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Finance.Integrity.DTOs;

namespace MyCondo.Application.Features.Finance.Integrity.Queries.GetFinancialIntegrityDashboard;

public sealed record GetFinancialIntegrityDashboardQuery : IRequest<FinancialIntegrityDashboardDto>, ILifecycleReadOperation;
