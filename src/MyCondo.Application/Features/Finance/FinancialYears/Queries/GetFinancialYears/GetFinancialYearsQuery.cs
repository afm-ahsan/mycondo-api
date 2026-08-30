using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Finance.FinancialYears.DTOs;

namespace MyCondo.Application.Features.Finance.FinancialYears.Queries.GetFinancialYears;

public sealed record GetFinancialYearsQuery : IRequest<List<FinancialYearDto>>, ILifecycleReadOperation;
