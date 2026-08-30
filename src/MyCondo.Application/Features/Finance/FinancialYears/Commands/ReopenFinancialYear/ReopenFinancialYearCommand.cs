using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Finance.FinancialYears.DTOs;

namespace MyCondo.Application.Features.Finance.FinancialYears.Commands.ReopenFinancialYear;

public sealed record ReopenFinancialYearCommand(Guid FinancialYearId) : IRequest<FinancialYearDto>, ILifecycleWriteOperation;
