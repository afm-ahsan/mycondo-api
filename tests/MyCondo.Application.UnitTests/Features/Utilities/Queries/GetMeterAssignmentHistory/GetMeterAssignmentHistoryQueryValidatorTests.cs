using AwesomeAssertions;
using FluentValidation.Results;
using MyCondo.Application.Features.Utilities.Queries.GetMeterAssignmentHistory;

namespace MyCondo.Application.UnitTests.Features.Utilities.Queries.GetMeterAssignmentHistory;

public class GetMeterAssignmentHistoryQueryValidatorTests
{
    private readonly GetMeterAssignmentHistoryQueryValidator _validator = new();

    [Fact]
    public void Valid_Query_Passes()
    {
        GetMeterAssignmentHistoryQuery query = new(Guid.NewGuid());

        ValidationResult result = _validator.Validate(query);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_MeterId_Fails()
    {
        GetMeterAssignmentHistoryQuery query = new(Guid.Empty);

        ValidationResult result = _validator.Validate(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(GetMeterAssignmentHistoryQuery.MeterId));
    }
}
