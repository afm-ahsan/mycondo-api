using AwesomeAssertions;
using FluentValidation.Results;
using MyCondo.Application.Features.Payroll.AttendanceRecords.Queries.GetAttendanceRecordsForTenant;

namespace MyCondo.Application.UnitTests.Features.Payroll.AttendanceRecords.Queries.GetAttendanceRecordsForTenant;

public class GetAttendanceRecordsForTenantQueryValidatorTests
{
    private readonly GetAttendanceRecordsForTenantQueryValidator _validator = new();

    [Fact]
    public void Valid_Query_With_No_Filters_Passes()
    {
        GetAttendanceRecordsForTenantQuery query = new(null, null, null, 1, 20);

        ValidationResult result = _validator.Validate(query);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Valid_Query_With_All_Filters_Passes()
    {
        GetAttendanceRecordsForTenantQuery query = new(
            DateOnly.FromDateTime(DateTime.UtcNow), Guid.NewGuid(), true, 1, 20);

        ValidationResult result = _validator.Validate(query);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Page_Below_One_Fails()
    {
        GetAttendanceRecordsForTenantQuery query = new(null, null, null, 0, 20);

        ValidationResult result = _validator.Validate(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(GetAttendanceRecordsForTenantQuery.Page));
    }

    [Fact]
    public void PageSize_Above_100_Fails()
    {
        GetAttendanceRecordsForTenantQuery query = new(null, null, null, 1, 101);

        ValidationResult result = _validator.Validate(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(GetAttendanceRecordsForTenantQuery.PageSize));
    }
}
