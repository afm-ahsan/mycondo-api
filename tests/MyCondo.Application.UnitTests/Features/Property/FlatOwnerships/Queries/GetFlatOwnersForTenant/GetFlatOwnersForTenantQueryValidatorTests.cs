using AwesomeAssertions;
using FluentValidation.Results;
using MyCondo.Application.Features.Property.FlatOwnerships.Queries.GetFlatOwnersForTenant;

namespace MyCondo.Application.UnitTests.Features.Property.FlatOwnerships.Queries.GetFlatOwnersForTenant;

public class GetFlatOwnersForTenantQueryValidatorTests
{
    private readonly GetFlatOwnersForTenantQueryValidator _validator = new();

    [Fact]
    public void Accepts_A_Query_With_No_Optional_Filters()
    {
        GetFlatOwnersForTenantQuery query = new(null, null);

        ValidationResult result = _validator.Validate(query);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("Active")]
    [InlineData("Ended")]
    public void Accepts_Every_Valid_Status_Value(string status)
    {
        GetFlatOwnersForTenantQuery query = new(null, status);

        ValidationResult result = _validator.Validate(query);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Rejects_An_Invalid_Status_Value()
    {
        GetFlatOwnersForTenantQuery query = new(null, "NotARealStatus");

        ValidationResult result = _validator.Validate(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(GetFlatOwnersForTenantQuery.Status));
    }

    [Fact]
    public void Rejects_PageSize_Above_100()
    {
        GetFlatOwnersForTenantQuery query = new(null, null, 1, 101);

        ValidationResult result = _validator.Validate(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(GetFlatOwnersForTenantQuery.PageSize));
    }
}
