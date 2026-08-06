using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Utilities.DTOs;
using MyCondo.Application.Features.Utilities.Mappings;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Utilities.Readings;

namespace MyCondo.Application.Features.Utilities.Commands.ReviewReading;

public sealed class ReviewReadingCommandHandler(
    IReadingRepository readings,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    IClock clock,
    ILogger<ReviewReadingCommandHandler> logger
) : IRequestHandler<ReviewReadingCommand, ReadingDto>
{
    public async ValueTask<ReadingDto> Handle(ReviewReadingCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        ReadingId id = new(command.ReadingId);
        Reading reading = await readings.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(Reading), command.ReadingId);
        if (reading.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(Reading), command.ReadingId);
        }

        reading.Review(currentUser.UserId, clock.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Reading {ReadingId} reviewed, tenant {TenantId}", id, tenantId);

        return reading.ToDto();
    }
}
