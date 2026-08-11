using AwesomeAssertions;
using FluentValidation.Results;
using MyCondo.Application.Features.Users.Queries.GetUsersForTenant;

namespace MyCondo.Application.UnitTests.Features.Users.Queries.GetUsersForTenant;

public class GetUsersForTenantQueryValidatorTests
{
    private readonly GetUsersForTenantQueryValidator _validator = new();

    [Fact]
    public void Accepts_A_Query_With_No_Optional_Filters()
    {
        GetUsersForTenantQuery query = new(null, null, null);

        ValidationResult result = _validator.Validate(query);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Rejects_PageSize_Above_100()
    {
        GetUsersForTenantQuery query = new(null, null, null, 1, 101);

        ValidationResult result = _validator.Validate(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(GetUsersForTenantQuery.PageSize));
    }

    [Fact]
    public void Rejects_Page_Below_1()
    {
        GetUsersForTenantQuery query = new(null, null, null, 0, 20);

        ValidationResult result = _validator.Validate(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(GetUsersForTenantQuery.Page));
    }
}
