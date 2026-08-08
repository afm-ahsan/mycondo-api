using AwesomeAssertions;
using FluentValidation.Results;
using MyCondo.Application.Features.Payments.Queries.GetPayments;

namespace MyCondo.Application.UnitTests.Features.Payments.Queries.GetPayments;

public class GetPaymentsQueryValidatorTests
{
    private readonly GetPaymentsQueryValidator _validator = new();

    [Fact]
    public void Valid_Query_With_No_Filters_Passes()
    {
        GetPaymentsQuery query = new(null, null, null, null, null, 1, 20);

        ValidationResult result = _validator.Validate(query);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Valid_Query_With_All_Filters_Passes()
    {
        GetPaymentsQuery query = new(
            Guid.NewGuid(), "Posted", "Cash", new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31), 1, 20);

        ValidationResult result = _validator.Validate(query);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Zero_Page_Fails()
    {
        GetPaymentsQuery query = new(null, null, null, null, null, 0, 20);

        ValidationResult result = _validator.Validate(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(GetPaymentsQuery.Page));
    }

    [Fact]
    public void PageSize_Over_100_Fails()
    {
        GetPaymentsQuery query = new(null, null, null, null, null, 1, 101);

        ValidationResult result = _validator.Validate(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(GetPaymentsQuery.PageSize));
    }

    [Fact]
    public void Invalid_Status_Fails()
    {
        GetPaymentsQuery query = new(null, "NotAStatus", null, null, null, 1, 20);

        ValidationResult result = _validator.Validate(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(GetPaymentsQuery.Status));
    }

    [Fact]
    public void Invalid_PaymentMethod_Fails()
    {
        GetPaymentsQuery query = new(null, null, "Bitcoin", null, null, 1, 20);

        ValidationResult result = _validator.Validate(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(GetPaymentsQuery.PaymentMethod));
    }
}
