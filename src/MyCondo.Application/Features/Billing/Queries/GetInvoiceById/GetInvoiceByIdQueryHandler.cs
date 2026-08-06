using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Billing.DTOs;
using MyCondo.Application.Features.Billing.Mappings;
using MyCondo.Domain.Features.Billing.Invoices;

namespace MyCondo.Application.Features.Billing.Queries.GetInvoiceById;

public sealed class GetInvoiceByIdQueryHandler(
    IInvoiceRepository invoices,
    ICurrentUserProvider currentUser
) : IRequestHandler<GetInvoiceByIdQuery, InvoiceDetailDto>
{
    public async ValueTask<InvoiceDetailDto> Handle(GetInvoiceByIdQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        InvoiceId id = new(query.InvoiceId);
        Invoice invoice = await invoices.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(Invoice), query.InvoiceId);
        if (invoice.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(Invoice), query.InvoiceId);
        }

        IReadOnlyList<InvoiceLine> lines = await invoices.GetLinesForInvoiceAsync(id, cancellationToken);

        return new InvoiceDetailDto(invoice.ToDto(), lines.Select(l => l.ToDto()).ToList());
    }
}
