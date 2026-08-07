using FluentValidation;

namespace MyCondo.Application.Features.Amenities.Queries.GetPoolDailyUsageReport;

public sealed class GetPoolDailyUsageReportQueryValidator : AbstractValidator<GetPoolDailyUsageReportQuery>
{
    public GetPoolDailyUsageReportQueryValidator()
    {
        RuleFor(x => x.FacilityId).NotEmpty();
        RuleFor(x => x.Date).NotEmpty();
    }
}
