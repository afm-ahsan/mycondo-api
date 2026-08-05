using FluentValidation;

namespace MyCondo.Application.Features.Security.AccessSessions.Queries.GetAccessSessionsForVehicle;

public sealed class GetAccessSessionsForVehicleQueryValidator : AbstractValidator<GetAccessSessionsForVehicleQuery>
{
    public GetAccessSessionsForVehicleQueryValidator()
    {
        RuleFor(x => x.VehicleId).NotEmpty();
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
