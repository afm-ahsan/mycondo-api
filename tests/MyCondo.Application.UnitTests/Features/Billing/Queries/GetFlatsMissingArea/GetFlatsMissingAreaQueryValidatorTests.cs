using AwesomeAssertions;
using FluentValidation.Results;
using MyCondo.Application.Features.Billing.Queries.GetFlatsMissingArea;

namespace MyCondo.Application.UnitTests.Features.Billing.Queries.GetFlatsMissingArea;

public class GetFlatsMissingAreaQueryValidatorTests
{
    private readonly GetFlatsMissingAreaQueryValidator _validator = new();

    [Fact]
    public void Valid_Query_Passes()
    {
        GetFlatsMissingAreaQuery query = new(Guid.NewGuid(), 1, 20);

        ValidationResult result = _validator.Validate(query);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_BuildingId_Fails()
    {
        GetFlatsMissingAreaQuery query = new(Guid.Empty, 1, 20);

        ValidationResult result = _validator.Validate(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(GetFlatsMissingAreaQuery.BuildingId));
    }

    [Fact]
    public void Zero_Page_Fails()
    {
        GetFlatsMissingAreaQuery query = new(Guid.NewGuid(), 0, 20);

        ValidationResult result = _validator.Validate(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(GetFlatsMissingAreaQuery.Page));
    }
}
