using FluentValidation;

namespace MyCondo.Application.Features.Operations.Commands.RecordStockMovement;

public sealed class RecordStockMovementCommandValidator : AbstractValidator<RecordStockMovementCommand>
{
    private static readonly string[] ValidKinds = ["Receipt", "Issue", "EmptyReturn"];

    public RecordStockMovementCommandValidator()
    {
        RuleFor(x => x.CylinderType).NotEmpty().MaximumLength(50);
        RuleFor(x => x.MovementKind).Must(k => ValidKinds.Contains(k))
            .WithMessage($"MovementKind must be one of: {string.Join(", ", ValidKinds)}.");
        RuleFor(x => x.Quantity).GreaterThan(0);
    }
}
