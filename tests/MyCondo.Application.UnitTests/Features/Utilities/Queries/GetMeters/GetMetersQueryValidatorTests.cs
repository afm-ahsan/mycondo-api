using AwesomeAssertions;
using FluentValidation.Results;
using MyCondo.Application.Features.Utilities.Queries.GetMeters;

namespace MyCondo.Application.UnitTests.Features.Utilities.Queries.GetMeters;

public class GetMetersQueryValidatorTests
{
    private readonly GetMetersQueryValidator _validator = new();

    [Fact]
    public void Valid_Query_Passes()
    {
        GetMetersQuery query = new(Guid.NewGuid(), "Electricity", 1, 20);

        ValidationResult result = _validator.Validate(query);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_BuildingId_Fails()
    {
        GetMetersQuery query = new(Guid.Empty, null, 1, 20);

        ValidationResult result = _validator.Validate(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(GetMetersQuery.BuildingId));
    }

    [Fact]
    public void Invalid_UtilityType_Fails()
    {
        GetMetersQuery query = new(Guid.NewGuid(), "Water", 1, 20);

        ValidationResult result = _validator.Validate(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(GetMetersQuery.UtilityType));
    }
}
