using FluentValidation;

namespace MyCondo.Application.Features.Amenities.Queries.GetFacilityUtilizationReport;

public sealed class GetFacilityUtilizationReportQueryValidator : AbstractValidator<GetFacilityUtilizationReportQuery>
{
    public GetFacilityUtilizationReportQueryValidator()
    {
        RuleFor(x => x.FromDate).NotEmpty();
        RuleFor(x => x.ToDate).GreaterThanOrEqualTo(x => x.FromDate)
            .WithMessage("ToDate must not precede FromDate.");
    }
}
