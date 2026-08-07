using FluentValidation;

namespace MyCondo.Application.Features.Amenities.Queries.GetBookingRevenueReport;

public sealed class GetBookingRevenueReportQueryValidator : AbstractValidator<GetBookingRevenueReportQuery>
{
    public GetBookingRevenueReportQueryValidator()
    {
        RuleFor(x => x.FromDate).NotEmpty();
        RuleFor(x => x.ToDate).GreaterThanOrEqualTo(x => x.FromDate)
            .WithMessage("ToDate must not precede FromDate.");
    }
}
