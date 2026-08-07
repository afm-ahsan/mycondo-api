using FluentValidation;

namespace MyCondo.Application.Features.Operations.Queries.GetGeneratorBreakdownRecords;

public sealed class GetGeneratorBreakdownRecordsQueryValidator : AbstractValidator<GetGeneratorBreakdownRecordsQuery>
{
    public GetGeneratorBreakdownRecordsQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
