using AwesomeAssertions;
using FluentValidation.Results;
using MyCondo.Application.Features.Billing.Queries.GetServiceChargeRules;

namespace MyCondo.Application.UnitTests.Features.Billing.Queries.GetServiceChargeRules;

public class GetServiceChargeRulesQueryValidatorTests
{
    private readonly GetServiceChargeRulesQueryValidator _validator = new();

    [Fact]
    public void Valid_Query_Passes()
    {
        GetServiceChargeRulesQuery query = new(Guid.NewGuid(), null, 1, 20);

        ValidationResult result = _validator.Validate(query);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_BuildingId_Fails()
    {
        GetServiceChargeRulesQuery query = new(Guid.Empty, null, 1, 20);

        ValidationResult result = _validator.Validate(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(GetServiceChargeRulesQuery.BuildingId));
    }

    [Fact]
    public void PageSize_Over_100_Fails()
    {
        GetServiceChargeRulesQuery query = new(Guid.NewGuid(), null, 1, 101);

        ValidationResult result = _validator.Validate(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(GetServiceChargeRulesQuery.PageSize));
    }
}
