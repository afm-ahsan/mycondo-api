using AwesomeAssertions;
using FluentValidation.Results;
using MyCondo.Application.Features.Utilities.Queries.GetConsumptionHistory;

namespace MyCondo.Application.UnitTests.Features.Utilities.Queries.GetConsumptionHistory;

public class GetConsumptionHistoryQueryValidatorTests
{
    private readonly GetConsumptionHistoryQueryValidator _validator = new();
    private static readonly DateOnly FromDate = new(2026, 1, 1);
    private static readonly DateOnly ToDate = new(2026, 3, 31);

    [Fact]
    public void Valid_Query_Passes()
    {
        GetConsumptionHistoryQuery query = new(Guid.NewGuid(), FromDate, ToDate);

        ValidationResult result = _validator.Validate(query);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_MeterId_Fails()
    {
        GetConsumptionHistoryQuery query = new(Guid.Empty, FromDate, ToDate);

        ValidationResult result = _validator.Validate(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(GetConsumptionHistoryQuery.MeterId));
    }

    [Fact]
    public void ToDate_Before_FromDate_Fails()
    {
        GetConsumptionHistoryQuery query = new(Guid.NewGuid(), ToDate, FromDate);

        ValidationResult result = _validator.Validate(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(GetConsumptionHistoryQuery.ToDate));
    }
}
