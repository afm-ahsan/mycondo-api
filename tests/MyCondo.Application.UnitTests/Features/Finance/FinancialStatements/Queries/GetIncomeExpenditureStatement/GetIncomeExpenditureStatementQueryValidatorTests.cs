using AwesomeAssertions;
using FluentValidation.Results;
using MyCondo.Application.Features.Finance.FinancialStatements.Queries.GetIncomeExpenditureStatement;

namespace MyCondo.Application.UnitTests.Features.Finance.FinancialStatements.Queries.GetIncomeExpenditureStatement;

public class GetIncomeExpenditureStatementQueryValidatorTests
{
    private readonly GetIncomeExpenditureStatementQueryValidator _validator = new();

    [Fact]
    public void EndDate_Before_StartDate_Fails_Validation()
    {
        GetIncomeExpenditureStatementQuery query = new(new DateOnly(2026, 6, 30), new DateOnly(2026, 6, 1), null);

        ValidationResult result = _validator.Validate(query);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void EndDate_Equal_To_StartDate_Passes_Validation()
    {
        GetIncomeExpenditureStatementQuery query = new(new DateOnly(2026, 6, 30), new DateOnly(2026, 6, 30), null);

        ValidationResult result = _validator.Validate(query);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void EndDate_After_StartDate_Passes_Validation()
    {
        GetIncomeExpenditureStatementQuery query = new(new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 30), null);

        ValidationResult result = _validator.Validate(query);

        result.IsValid.Should().BeTrue();
    }
}
