using FluentValidation;

namespace MyCondo.Application.Features.Operations.Queries.GetGeneratorServiceRecords;

public sealed class GetGeneratorServiceRecordsQueryValidator : AbstractValidator<GetGeneratorServiceRecordsQuery>
{
    public GetGeneratorServiceRecordsQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
