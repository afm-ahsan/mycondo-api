using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Utilities.DTOs;

namespace MyCondo.Application.Features.Utilities.Commands.InstallMeter;

public sealed record InstallMeterCommand(Guid BuildingId, string UtilityType, string MeterNumber) : IRequest<MeterDto>, IRequiresFeature
{
    // UtilityType is required here (unlike the nullable filter on GetMetersQuery), so the feature this
    // call actually uses is determinate from the request itself — no DB lookup needed.
    public string FeatureKey => UtilityType == "Gas" ? "utilities.gas" : "utilities.electricity";
}
