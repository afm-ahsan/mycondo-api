using Mediator;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Billing.DTOs;
using MyCondo.Application.Features.Billing.Services;
using MyCondo.Application.Features.Finance.Services;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Billing.Invoices;
using MyCondo.Domain.Features.Billing.InvoiceSequences;
using MyCondo.Domain.Features.Billing.ServiceChargeRules;
using MyCondo.Domain.Features.Payments.Ledger;
using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Domain.Features.Property.Flats;

namespace MyCondo.Application.Features.Billing.Commands.GenerateInvoiceBatch;

/// <summary>
/// Batch-generates one invoice per flat for a billing period, one invoice per (flat, period) with a
/// line per applicable rule. Atomic at the invoice level, not the batch level — each flat's invoice
/// (sequence bump + invoice/line inserts + ledger posting) commits in its own transaction, so one
/// flat's failure never rolls back another flat's already-committed invoice. See plan §10.
/// </summary>
public sealed class GenerateInvoiceBatchCommandHandler(
    IBuildingRepository buildings,
    IFlatRepository flats,
    IServiceChargeRuleRepository rules,
    IInvoiceRepository invoices,
    IInvoiceSequenceRepository sequences,
    IFinancialPostingService financialPosting,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUser,
    IClock clock,
    ILogger<GenerateInvoiceBatchCommandHandler> logger
) : IRequestHandler<GenerateInvoiceBatchCommand, GenerateInvoiceBatchResultDto>
{
    public async ValueTask<GenerateInvoiceBatchResultDto> Handle(
        GenerateInvoiceBatchCommand command, CancellationToken cancellationToken)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            throw new ForbiddenException("Authentication required.");
        }

        BuildingId buildingId = new(command.BuildingId);
        Building building = await buildings.GetByIdAsync(buildingId, cancellationToken)
            ?? throw new NotFoundException(nameof(Building), command.BuildingId);
        if (building.TenantId != tenantId)
        {
            throw new NotFoundException(nameof(Building), command.BuildingId);
        }

        IReadOnlyList<ServiceChargeRule> applicableRules = await rules.GetApplicableRulesAsync(
            tenantId, buildingId, command.PeriodStart, command.PeriodEnd, cancellationToken);

        if (applicableRules.Count == 0)
        {
            logger.LogInformation(
                "No applicable service charge rules for building {BuildingId} covering {PeriodStart}..{PeriodEnd}, tenant {TenantId} — nothing to generate.",
                buildingId, command.PeriodStart, command.PeriodEnd, tenantId);
            return new GenerateInvoiceBatchResultDto(0, 0, 0, 0, 0, [], [], []);
        }

        IReadOnlyList<Flat> buildingFlats = await flats.GetAllForBuildingAsync(tenantId, buildingId, cancellationToken);

        int generatedCount = 0;
        int alreadyExistingCount = 0;
        int skippedCount = 0;
        int failedCount = 0;
        List<Guid> generatedInvoiceIds = [];
        List<BatchItemOutcomeDto> skippedItems = [];
        List<BatchItemOutcomeDto> failedItems = [];

        foreach (Flat flat in buildingFlats)
        {
            bool alreadyExists = await invoices.ExistsForFlatAndPeriodAsync(
                tenantId, flat.Id, command.PeriodStart, command.PeriodEnd, InvoiceSource.ServiceCharge, cancellationToken);
            if (alreadyExists)
            {
                alreadyExistingCount++;
                continue;
            }

            List<ServiceChargeRule> flatRules = applicableRules
                .Where(r => r.UnitTypeFilter is null || r.UnitTypeFilter == flat.FlatType)
                .ToList();

            if (flatRules.Count == 0)
            {
                skippedCount++;
                skippedItems.Add(new BatchItemOutcomeDto(flat.Id.Value, "No applicable service charge rule for this flat's unit type"));
                continue;
            }

            List<InvoiceLineInput> lineInputs = [];
            List<string> lineSkipReasons = [];
            foreach (ServiceChargeRule rule in flatRules)
            {
                ServiceChargeCalculationResult result = ServiceChargeCalculator.Calculate(rule, flat);
                if (result.IsSuccess)
                {
                    lineInputs.Add(result.Line!);
                }
                else
                {
                    lineSkipReasons.Add($"{rule.Name}: {result.SkipReason}");
                }
            }

            if (lineInputs.Count == 0)
            {
                skippedCount++;
                skippedItems.Add(new BatchItemOutcomeDto(flat.Id.Value, string.Join("; ", lineSkipReasons)));
                continue;
            }

            if (lineSkipReasons.Count > 0)
            {
                logger.LogWarning(
                    "Flat {FlatId} invoiced with {LineCount} of {RuleCount} applicable rules — skipped: {SkipReasons}",
                    flat.Id, lineInputs.Count, flatRules.Count, string.Join("; ", lineSkipReasons));
            }

            IUnitOfWorkTransaction? transaction = null;
            try
            {
                transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);

                DateTimeOffset nowUtc = clock.UtcNow;
                DateOnly invoiceDate = DateOnly.FromDateTime(nowUtc.UtcDateTime);
                DateOnly dueDate = command.PeriodEnd;

                int seq = await sequences.GetNextValueAsync(tenantId, buildingId, invoiceDate.Year, cancellationToken);
                string invoiceNumber = $"INV-{building.Code}-{invoiceDate.Year}-{seq:D6}";

                decimal subtotal = lineInputs.Sum(l => l.LineAmount);
                string description = $"Service charge invoice {invoiceNumber} for flat {flat.FlatNumber}, period {command.PeriodStart}..{command.PeriodEnd}";

                FinancialPostingLine[] postingLines =
                [
                    new FinancialPostingLine(LedgerAccountType.ResidentReceivable, flat.Id, LedgerDirection.Debit, subtotal),
                    new FinancialPostingLine(LedgerAccountType.AssociationRevenue, null, LedgerDirection.Credit, subtotal),
                ];

                FinancialPostingResult posted = await financialPosting.PostAsync(
                    new FinancialPostingRequest(tenantId, invoiceDate, description, "Invoice", null, postingLines),
                    cancellationToken);

                (Invoice invoice, IReadOnlyList<InvoiceLine> lines) = Invoice.Issue(
                    tenantId, buildingId, flat.Id, invoiceNumber, InvoiceSource.ServiceCharge, command.PeriodStart,
                    command.PeriodEnd, invoiceDate, dueDate, lineInputs, posted.Posting.Id, nowUtc);

                invoices.Add(invoice);
                invoices.AddLines(lines);

                await unitOfWork.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                generatedCount++;
                generatedInvoiceIds.Add(invoice.Id.Value);
            }
            catch (Exception ex)
            {
                if (transaction is not null)
                {
                    await transaction.RollbackAsync(cancellationToken);
                }

                failedCount++;
                failedItems.Add(new BatchItemOutcomeDto(flat.Id.Value, ex.Message));

                logger.LogError(
                    ex, "Failed to generate invoice for flat {FlatId} in building {BuildingId}, tenant {TenantId}",
                    flat.Id, buildingId, tenantId);
            }
            finally
            {
                if (transaction is not null)
                {
                    await transaction.DisposeAsync();
                }
            }
        }

        logger.LogInformation(
            "Invoice batch for building {BuildingId} tenant {TenantId}: requested {Requested}, generated {Generated}, already-existing {AlreadyExisting}, skipped {Skipped}, failed {Failed}",
            buildingId, tenantId, buildingFlats.Count, generatedCount, alreadyExistingCount, skippedCount, failedCount);

        return new GenerateInvoiceBatchResultDto(
            buildingFlats.Count, generatedCount, alreadyExistingCount, skippedCount, failedCount,
            generatedInvoiceIds, skippedItems, failedItems);
    }
}
