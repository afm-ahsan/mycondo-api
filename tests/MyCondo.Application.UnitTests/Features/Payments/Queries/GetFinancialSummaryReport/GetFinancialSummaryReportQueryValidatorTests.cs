using AwesomeAssertions;
using FluentValidation.Results;
using MyCondo.Application.Features.Payments.Queries.GetFinancialSummaryReport;

namespace MyCondo.Application.UnitTests.Features.Payments.Queries.GetFinancialSummaryReport;

public class GetFinancialSummaryReportQueryValidatorTests
{
    private readonly GetFinancialSummaryReportQueryValidator _validator = new();

    [Fact]
    public void Valid_Query_Passes()
    {
        GetFinancialSummaryReportQuery query = new(null, new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 31));

        ValidationResult result = _validator.Validate(query);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Building_Scoped_Query_Passes()
    {
        GetFinancialSummaryReportQuery query = new(Guid.NewGuid(), new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 31));

        ValidationResult result = _validator.Validate(query);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ToDate_Before_FromDate_Fails()
    {
        GetFinancialSummaryReportQuery query = new(null, new DateOnly(2026, 8, 31), new DateOnly(2026, 8, 1));

        ValidationResult result = _validator.Validate(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(GetFinancialSummaryReportQuery.ToDate));
    }

    [Fact]
    public void FromDate_Equal_To_ToDate_Passes()
    {
        GetFinancialSummaryReportQuery query = new(null, new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 1));

        ValidationResult result = _validator.Validate(query);

        result.IsValid.Should().BeTrue();
    }
}
