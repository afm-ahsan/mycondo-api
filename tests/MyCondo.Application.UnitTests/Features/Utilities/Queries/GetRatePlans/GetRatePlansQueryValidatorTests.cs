using AwesomeAssertions;
using FluentValidation.Results;
using MyCondo.Application.Features.Utilities.Queries.GetRatePlans;

namespace MyCondo.Application.UnitTests.Features.Utilities.Queries.GetRatePlans;

public class GetRatePlansQueryValidatorTests
{
    private readonly GetRatePlansQueryValidator _validator = new();

    [Fact]
    public void Valid_Query_Passes()
    {
        GetRatePlansQuery query = new(Guid.NewGuid(), "Gas", 1, 20);

        ValidationResult result = _validator.Validate(query);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_BuildingId_Fails()
    {
        GetRatePlansQuery query = new(Guid.Empty, null, 1, 20);

        ValidationResult result = _validator.Validate(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(GetRatePlansQuery.BuildingId));
    }

    [Fact]
    public void PageSize_Over_100_Fails()
    {
        GetRatePlansQuery query = new(Guid.NewGuid(), null, 1, 101);

        ValidationResult result = _validator.Validate(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(GetRatePlansQuery.PageSize));
    }
}
