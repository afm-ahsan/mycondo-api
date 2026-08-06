using AwesomeAssertions;
using FluentValidation.Results;
using MyCondo.Application.Features.Utilities.Queries.GetReadings;

namespace MyCondo.Application.UnitTests.Features.Utilities.Queries.GetReadings;

public class GetReadingsQueryValidatorTests
{
    private readonly GetReadingsQueryValidator _validator = new();

    [Fact]
    public void Valid_Query_Passes()
    {
        GetReadingsQuery query = new(Guid.NewGuid(), null, "Finalized", 1, 20);

        ValidationResult result = _validator.Validate(query);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Null_Filters_Are_Valid()
    {
        GetReadingsQuery query = new(null, null, null, 1, 20);

        ValidationResult result = _validator.Validate(query);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Invalid_Status_Fails()
    {
        GetReadingsQuery query = new(null, null, "NotAStatus", 1, 20);

        ValidationResult result = _validator.Validate(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(GetReadingsQuery.Status));
    }
}
