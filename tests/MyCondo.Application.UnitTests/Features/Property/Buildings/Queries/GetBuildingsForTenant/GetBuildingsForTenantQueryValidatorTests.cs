using AwesomeAssertions;
using FluentValidation.Results;
using MyCondo.Application.Features.Property.Buildings.Queries.GetBuildingsForTenant;

namespace MyCondo.Application.UnitTests.Features.Property.Buildings.Queries.GetBuildingsForTenant;

public class GetBuildingsForTenantQueryValidatorTests
{
    private readonly GetBuildingsForTenantQueryValidator _validator = new();

    [Fact]
    public void Valid_Query_Passes()
    {
        GetBuildingsForTenantQuery query = new("tower", 1, 20);

        ValidationResult result = _validator.Validate(query);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Page_Below_One_Fails()
    {
        GetBuildingsForTenantQuery query = new(null, 0, 20);

        ValidationResult result = _validator.Validate(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(GetBuildingsForTenantQuery.Page));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void PageSize_Outside_Range_Fails(int pageSize)
    {
        GetBuildingsForTenantQuery query = new(null, 1, pageSize);

        ValidationResult result = _validator.Validate(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(GetBuildingsForTenantQuery.PageSize));
    }
}
