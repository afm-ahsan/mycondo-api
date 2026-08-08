using AwesomeAssertions;
using FluentValidation.Results;
using MyCondo.Application.Features.Utilities.Queries.GetConsumptionSummaryReport;

namespace MyCondo.Application.UnitTests.Features.Utilities.Queries.GetConsumptionSummaryReport;

public class GetConsumptionSummaryReportQueryValidatorTests
{
    private readonly GetConsumptionSummaryReportQueryValidator _validator = new();

    [Fact]
    public void Valid_Query_Passes()
    {
        GetConsumptionSummaryReportQuery query = new(null, "Electricity", new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 31));

        ValidationResult result = _validator.Validate(query);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Null_UtilityType_Is_Valid()
    {
        GetConsumptionSummaryReportQuery query = new(null, null, new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 31));

        ValidationResult result = _validator.Validate(query);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Invalid_UtilityType_Fails()
    {
        GetConsumptionSummaryReportQuery query = new(null, "Water", new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 31));

        ValidationResult result = _validator.Validate(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(GetConsumptionSummaryReportQuery.UtilityType));
    }

    [Fact]
    public void ToDate_Before_FromDate_Fails()
    {
        GetConsumptionSummaryReportQuery query = new(null, null, new DateOnly(2026, 8, 31), new DateOnly(2026, 8, 1));

        ValidationResult result = _validator.Validate(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(GetConsumptionSummaryReportQuery.ToDate));
    }
}
