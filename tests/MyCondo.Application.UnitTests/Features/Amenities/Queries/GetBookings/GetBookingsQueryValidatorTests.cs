using AwesomeAssertions;
using FluentValidation.Results;
using MyCondo.Application.Features.Amenities.Queries.GetBookings;

namespace MyCondo.Application.UnitTests.Features.Amenities.Queries.GetBookings;

public class GetBookingsQueryValidatorTests
{
    private readonly GetBookingsQueryValidator _validator = new();

    private static GetBookingsQuery ValidQuery(
        string? status = null, string? paymentStatus = null, DateTimeOffset? fromDate = null,
        DateTimeOffset? toDate = null, int page = 1, int pageSize = 20) =>
        new(null, null, status, null, null, paymentStatus, fromDate, toDate, page, pageSize);

    [Fact]
    public void Accepts_A_Query_With_No_Optional_Filters()
    {
        ValidationResult result = _validator.Validate(ValidQuery());

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("NotRequired")]
    [InlineData("AwaitingPayment")]
    [InlineData("Paid")]
    public void Accepts_Every_Valid_PaymentStatus_Value(string paymentStatus)
    {
        ValidationResult result = _validator.Validate(ValidQuery(paymentStatus: paymentStatus));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Rejects_An_Invalid_PaymentStatus_Value()
    {
        ValidationResult result = _validator.Validate(ValidQuery(paymentStatus: "Refunded"));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(GetBookingsQuery.PaymentStatus));
    }

    [Fact]
    public void Rejects_An_Invalid_Status_Value()
    {
        ValidationResult result = _validator.Validate(ValidQuery(status: "Deleted"));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(GetBookingsQuery.Status));
    }

    [Fact]
    public void Accepts_A_ToDate_Equal_To_FromDate()
    {
        DateTimeOffset date = new(2026, 9, 15, 0, 0, 0, TimeSpan.Zero);

        ValidationResult result = _validator.Validate(ValidQuery(fromDate: date, toDate: date));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Accepts_A_ToDate_After_FromDate()
    {
        DateTimeOffset fromDate = new(2026, 9, 1, 0, 0, 0, TimeSpan.Zero);
        DateTimeOffset toDate = new(2026, 10, 1, 0, 0, 0, TimeSpan.Zero);

        ValidationResult result = _validator.Validate(ValidQuery(fromDate: fromDate, toDate: toDate));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Rejects_A_ToDate_Before_FromDate()
    {
        DateTimeOffset fromDate = new(2026, 10, 1, 0, 0, 0, TimeSpan.Zero);
        DateTimeOffset toDate = new(2026, 9, 1, 0, 0, 0, TimeSpan.Zero);

        ValidationResult result = _validator.Validate(ValidQuery(fromDate: fromDate, toDate: toDate));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(GetBookingsQuery.ToDate));
    }

    [Fact]
    public void Accepts_FromDate_Without_ToDate()
    {
        ValidationResult result = _validator.Validate(ValidQuery(fromDate: DateTimeOffset.UtcNow));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Accepts_ToDate_Without_FromDate()
    {
        ValidationResult result = _validator.Validate(ValidQuery(toDate: DateTimeOffset.UtcNow));

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Rejects_A_Page_Below_One(int page)
    {
        ValidationResult result = _validator.Validate(ValidQuery(page: page));

        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void Rejects_A_PageSize_Outside_The_Allowed_Range(int pageSize)
    {
        ValidationResult result = _validator.Validate(ValidQuery(pageSize: pageSize));

        result.IsValid.Should().BeFalse();
    }
}
