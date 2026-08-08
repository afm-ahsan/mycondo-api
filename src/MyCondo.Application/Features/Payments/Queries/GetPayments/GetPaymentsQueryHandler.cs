using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Payments.DTOs;
using MyCondo.Application.Features.Payments.Mappings;
using MyCondo.Domain.Common;
using MyCondo.Domain.Features.Payments.Payments;
using MyCondo.Domain.Features.Property.Flats;

namespace MyCondo.Application.Features.Payments.Queries.GetPayments;

/// <summary>Tenant-wide payment list — allocations are intentionally omitted here (no N+1 invoice
/// lookups per row); use <c>GetPaymentByIdQuery</c> for the full allocation/invoice-number
/// breakdown of one payment.</summary>
public sealed class GetPaymentsQueryHandler(
    IPaymentRepository payments,
    ICurrentUserProvider currentUser
) : IRequestHandler<GetPaymentsQuery, PagedResult<PaymentDto>>
{
    public async ValueTask<PagedResult<PaymentDto>> Handle(GetPaymentsQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        FlatId? flatId = query.FlatId is Guid rawFlatId ? new FlatId(rawFlatId) : null;
        PaymentStatus? status = query.Status is null ? null : Enum.Parse<PaymentStatus>(query.Status);
        PaymentMethod? paymentMethod = query.PaymentMethod is null ? null : Enum.Parse<PaymentMethod>(query.PaymentMethod);

        PagedResult<Payment> result = await payments.SearchAsync(
            tenantId, flatId, status, paymentMethod, query.FromDate, query.ToDate, query.Page, query.PageSize, cancellationToken);

        List<PaymentDto> items = result.Items.Select(p => p.ToDto()).ToList();

        return new PagedResult<PaymentDto>(items, result.Page, result.PageSize, result.Total);
    }
}
