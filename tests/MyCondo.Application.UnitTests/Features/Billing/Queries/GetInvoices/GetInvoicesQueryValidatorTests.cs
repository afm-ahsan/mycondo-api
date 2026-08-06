using AwesomeAssertions;
using FluentValidation.Results;
using MyCondo.Application.Features.Billing.Queries.GetInvoices;

namespace MyCondo.Application.UnitTests.Features.Billing.Queries.GetInvoices;

public class GetInvoicesQueryValidatorTests
{
    private readonly GetInvoicesQueryValidator _validator = new();

    [Fact]
    public void Valid_Query_Passes()
    {
        GetInvoicesQuery query = new(Guid.NewGuid(), null, "Issued", 1, 20);

        ValidationResult result = _validator.Validate(query);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Null_Filters_Are_Valid()
    {
        GetInvoicesQuery query = new(null, null, null, 1, 20);

        ValidationResult result = _validator.Validate(query);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Invalid_Status_Fails()
    {
        GetInvoicesQuery query = new(null, null, "NotAStatus", 1, 20);

        ValidationResult result = _validator.Validate(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(GetInvoicesQuery.Status));
    }

    [Fact]
    public void PageSize_Over_100_Fails()
    {
        GetInvoicesQuery query = new(null, null, null, 1, 101);

        ValidationResult result = _validator.Validate(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(GetInvoicesQuery.PageSize));
    }
}
