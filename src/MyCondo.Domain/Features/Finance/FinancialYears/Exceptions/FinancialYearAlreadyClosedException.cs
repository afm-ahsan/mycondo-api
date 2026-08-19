using MyCondo.Domain.Exceptions;

namespace MyCondo.Domain.Features.Finance.FinancialYears.Exceptions;

public sealed class FinancialYearAlreadyClosedException(FinancialYearId id)
    : DomainException($"Financial year {id} is already closed.");
