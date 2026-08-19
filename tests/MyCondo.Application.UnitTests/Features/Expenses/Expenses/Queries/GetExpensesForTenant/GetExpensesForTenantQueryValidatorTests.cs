using AwesomeAssertions;
using FluentValidation.Results;
using MyCondo.Application.Features.Expenses.Expenses.Queries.GetExpensesForTenant;

namespace MyCondo.Application.UnitTests.Features.Expenses.Expenses.Queries.GetExpensesForTenant;

public class GetExpensesForTenantQueryValidatorTests
{
    private readonly GetExpensesForTenantQueryValidator _validator = new();

    [Fact]
    public void Accepts_A_Query_With_No_Optional_Filters()
    {
        GetExpensesForTenantQuery query = new(null, null, null, null, null, null);

        ValidationResult result = _validator.Validate(query);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Rejects_An_Invalid_Status_Value()
    {
        GetExpensesForTenantQuery query = new(null, null, null, "NotARealStatus", null, null);

        ValidationResult result = _validator.Validate(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(GetExpensesForTenantQuery.Status));
    }

    [Fact]
    public void Rejects_ToDate_Before_FromDate()
    {
        GetExpensesForTenantQuery query = new(null, null, null, null, new DateOnly(2026, 8, 10), new DateOnly(2026, 8, 1));

        ValidationResult result = _validator.Validate(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(GetExpensesForTenantQuery.ToDate));
    }
}
