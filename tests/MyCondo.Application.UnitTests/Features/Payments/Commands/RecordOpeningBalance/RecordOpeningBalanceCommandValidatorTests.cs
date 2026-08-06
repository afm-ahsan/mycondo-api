using AwesomeAssertions;
using FluentValidation.Results;
using MyCondo.Application.Features.Payments.Commands.RecordOpeningBalance;

namespace MyCondo.Application.UnitTests.Features.Payments.Commands.RecordOpeningBalance;

public class RecordOpeningBalanceCommandValidatorTests
{
    private readonly RecordOpeningBalanceCommandValidator _validator = new();
    private static readonly DateOnly BusinessDate = DateOnly.FromDateTime(DateTime.UtcNow);

    [Fact]
    public void Valid_Command_Passes()
    {
        RecordOpeningBalanceCommand command = new(Guid.NewGuid(), 1000m, BusinessDate, "Migrated balance");

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_FlatId_Fails()
    {
        RecordOpeningBalanceCommand command = new(Guid.Empty, 1000m, BusinessDate, null);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RecordOpeningBalanceCommand.FlatId));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public void NonPositive_Amount_Fails(decimal amount)
    {
        RecordOpeningBalanceCommand command = new(Guid.NewGuid(), amount, BusinessDate, null);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RecordOpeningBalanceCommand.Amount));
    }
}
