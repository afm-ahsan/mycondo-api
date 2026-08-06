using AwesomeAssertions;
using FluentValidation.Results;
using MyCondo.Application.Features.Payments.Queries.GetAccountBalance;

namespace MyCondo.Application.UnitTests.Features.Payments.Queries.GetAccountBalance;

public class GetAccountBalanceQueryValidatorTests
{
    private readonly GetAccountBalanceQueryValidator _validator = new();

    [Fact]
    public void Valid_Query_Passes()
    {
        GetAccountBalanceQuery query = new(Guid.NewGuid());

        ValidationResult result = _validator.Validate(query);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_FlatId_Fails()
    {
        GetAccountBalanceQuery query = new(Guid.Empty);

        ValidationResult result = _validator.Validate(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(GetAccountBalanceQuery.FlatId));
    }
}
