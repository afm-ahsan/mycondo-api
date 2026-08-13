using AwesomeAssertions;
using FluentValidation.Results;
using MyCondo.Application.Features.Property.Flats.Queries.GetFlatsForTenant;

namespace MyCondo.Application.UnitTests.Features.Property.Flats.Queries.GetFlatsForTenant;

public class GetFlatsForTenantQueryValidatorTests
{
    private readonly GetFlatsForTenantQueryValidator _validator = new();

    [Fact]
    public void Valid_Query_Passes()
    {
        GetFlatsForTenantQuery query = new("A-1", Guid.NewGuid(), 1, 20);

        ValidationResult result = _validator.Validate(query);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Valid_Query_Without_BuildingId_Passes()
    {
        GetFlatsForTenantQuery query = new(null, null, 1, 20);

        ValidationResult result = _validator.Validate(query);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Page_Below_One_Fails()
    {
        GetFlatsForTenantQuery query = new(null, null, 0, 20);

        ValidationResult result = _validator.Validate(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(GetFlatsForTenantQuery.Page));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void PageSize_Outside_Range_Fails(int pageSize)
    {
        GetFlatsForTenantQuery query = new(null, null, 1, pageSize);

        ValidationResult result = _validator.Validate(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(GetFlatsForTenantQuery.PageSize));
    }
}
