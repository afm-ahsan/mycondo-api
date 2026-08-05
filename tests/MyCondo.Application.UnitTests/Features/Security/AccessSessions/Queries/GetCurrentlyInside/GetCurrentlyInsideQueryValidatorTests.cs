using AwesomeAssertions;
using FluentValidation.Results;
using MyCondo.Application.Features.Security.AccessSessions.Queries.GetCurrentlyInside;

namespace MyCondo.Application.UnitTests.Features.Security.AccessSessions.Queries.GetCurrentlyInside;

public class GetCurrentlyInsideQueryValidatorTests
{
    private readonly GetCurrentlyInsideQueryValidator _validator = new();

    [Fact]
    public void Valid_Query_With_No_Category_Passes()
    {
        GetCurrentlyInsideQuery query = new(null, 1, 20);

        ValidationResult result = _validator.Validate(query);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Valid_Category_Passes()
    {
        GetCurrentlyInsideQuery query = new("Guest", 1, 20);

        ValidationResult result = _validator.Validate(query);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Invalid_Category_Fails()
    {
        GetCurrentlyInsideQuery query = new("NotACategory", 1, 20);

        ValidationResult result = _validator.Validate(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(GetCurrentlyInsideQuery.Category));
    }
}
