using FluentValidation;

namespace MyCondo.Application.Features.Utilities.Queries.GetMeterAssignmentHistory;

public sealed class GetMeterAssignmentHistoryQueryValidator : AbstractValidator<GetMeterAssignmentHistoryQuery>
{
    public GetMeterAssignmentHistoryQueryValidator()
    {
        RuleFor(x => x.MeterId).NotEmpty();
    }
}
