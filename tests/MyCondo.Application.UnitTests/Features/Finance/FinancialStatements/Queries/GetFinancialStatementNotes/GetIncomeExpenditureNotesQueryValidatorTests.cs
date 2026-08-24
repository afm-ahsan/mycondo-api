using AwesomeAssertions;
using FluentValidation.Results;
using MyCondo.Application.Features.Finance.FinancialStatements.Queries.GetIncomeExpenditureNotes;

namespace MyCondo.Application.UnitTests.Features.Finance.FinancialStatements.Queries.GetFinancialStatementNotes;

public class GetIncomeExpenditureNotesQueryValidatorTests
{
    private readonly GetIncomeExpenditureNotesQueryValidator _validator = new();

    [Fact]
    public void EndDate_Before_StartDate_Is_Rejected()
    {
        ValidationResult result = _validator.Validate(
            new GetIncomeExpenditureNotesQuery(new DateOnly(2026, 6, 30), new DateOnly(2026, 6, 1), null, null));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(GetIncomeExpenditureNotesQuery.EndDate));
    }

    [Fact]
    public void A_Single_Day_Period_Is_Valid()
    {
        ValidationResult result = _validator.Validate(
            new GetIncomeExpenditureNotesQuery(new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 1), null, null));

        result.IsValid.Should().BeTrue();
    }
}
