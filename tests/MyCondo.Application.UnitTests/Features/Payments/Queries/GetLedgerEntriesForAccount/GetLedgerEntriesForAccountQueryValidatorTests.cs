using AwesomeAssertions;
using FluentValidation.Results;
using MyCondo.Application.Features.Payments.Queries.GetLedgerEntriesForAccount;

namespace MyCondo.Application.UnitTests.Features.Payments.Queries.GetLedgerEntriesForAccount;

public class GetLedgerEntriesForAccountQueryValidatorTests
{
    private readonly GetLedgerEntriesForAccountQueryValidator _validator = new();

    [Fact]
    public void Valid_Query_Passes()
    {
        GetLedgerEntriesForAccountQuery query = new(Guid.NewGuid(), null, null, null, 1, 20);

        ValidationResult result = _validator.Validate(query);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Zero_Page_Fails()
    {
        GetLedgerEntriesForAccountQuery query = new(Guid.NewGuid(), null, null, null, 0, 20);

        ValidationResult result = _validator.Validate(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(GetLedgerEntriesForAccountQuery.Page));
    }

    [Fact]
    public void PageSize_Over_100_Fails()
    {
        GetLedgerEntriesForAccountQuery query = new(Guid.NewGuid(), null, null, null, 1, 101);

        ValidationResult result = _validator.Validate(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(GetLedgerEntriesForAccountQuery.PageSize));
    }

    [Fact]
    public void Valid_Query_With_Date_Range_And_ReferenceType_Passes()
    {
        GetLedgerEntriesForAccountQuery query = new(
            Guid.NewGuid(), new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31), "Payment", 1, 20);

        ValidationResult result = _validator.Validate(query);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ToDate_Before_FromDate_Fails()
    {
        GetLedgerEntriesForAccountQuery query = new(
            Guid.NewGuid(), new DateOnly(2026, 1, 31), new DateOnly(2026, 1, 1), null, 1, 20);

        ValidationResult result = _validator.Validate(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(GetLedgerEntriesForAccountQuery.ToDate));
    }
}
