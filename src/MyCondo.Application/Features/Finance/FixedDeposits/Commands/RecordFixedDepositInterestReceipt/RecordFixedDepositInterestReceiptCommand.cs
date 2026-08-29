using Mediator;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Features.Finance.FixedDeposits.DTOs;

namespace MyCondo.Application.Features.Finance.FixedDeposits.Commands.RecordFixedDepositInterestReceipt;

public sealed record RecordFixedDepositInterestReceiptCommand(
    Guid FixedDepositId,
    DateOnly? AccountingDate,
    decimal GrossAmount,
    decimal DeductionAmount,
    Guid ReceivingFinancialAccountId,
    string? ReferenceNumber,
    string? Notes) : IRequest<FixedDepositInterestReceiptDto>, IRequiresFeature, ILifecycleWriteOperation
{
    public string FeatureKey => "finance.fixed_deposits";
}
