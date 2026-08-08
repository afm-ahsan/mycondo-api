using FluentValidation;
using MyCondo.Domain.Features.Amenities.Bookings;

namespace MyCondo.Application.Features.Amenities.Queries.GetBookings;

public sealed class GetBookingsQueryValidator : AbstractValidator<GetBookingsQuery>
{
    public GetBookingsQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.Status).Must(BeAValidStatus!).When(x => x.Status is not null)
            .WithMessage($"Status must be one of: {string.Join(", ", Enum.GetNames<BookingStatus>())}.");
        RuleFor(x => x.PaymentStatus).Must(BeAValidPaymentStatus!).When(x => x.PaymentStatus is not null)
            .WithMessage($"PaymentStatus must be one of: {string.Join(", ", Enum.GetNames<BookingPaymentStatus>())}.");
        RuleFor(x => x.ToDate).GreaterThanOrEqualTo(x => x.FromDate!.Value)
            .When(x => x.FromDate is not null && x.ToDate is not null)
            .WithMessage("ToDate must not be before FromDate.");
    }

    private static bool BeAValidStatus(string value) => Enum.TryParse<BookingStatus>(value, out _);

    private static bool BeAValidPaymentStatus(string value) => Enum.TryParse<BookingPaymentStatus>(value, out _);
}
