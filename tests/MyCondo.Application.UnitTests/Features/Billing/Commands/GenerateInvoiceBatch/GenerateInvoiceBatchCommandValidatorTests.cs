using AwesomeAssertions;
using FluentValidation.Results;
using MyCondo.Application.Features.Billing.Commands.GenerateInvoiceBatch;

namespace MyCondo.Application.UnitTests.Features.Billing.Commands.GenerateInvoiceBatch;

public class GenerateInvoiceBatchCommandValidatorTests
{
    private readonly GenerateInvoiceBatchCommandValidator _validator = new();

    [Fact]
    public void Valid_Command_Passes()
    {
        GenerateInvoiceBatchCommand command = new(Guid.NewGuid(), new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 31));

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_BuildingId_Fails()
    {
        GenerateInvoiceBatchCommand command = new(Guid.Empty, new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 31));

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(GenerateInvoiceBatchCommand.BuildingId));
    }

    [Fact]
    public void PeriodEnd_Before_PeriodStart_Fails()
    {
        GenerateInvoiceBatchCommand command = new(Guid.NewGuid(), new DateOnly(2026, 3, 31), new DateOnly(2026, 3, 1));

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(GenerateInvoiceBatchCommand.PeriodEnd));
    }
}
