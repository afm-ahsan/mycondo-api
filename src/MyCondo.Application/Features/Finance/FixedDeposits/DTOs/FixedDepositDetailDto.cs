namespace MyCondo.Application.Features.Finance.FixedDeposits.DTOs;

public sealed record FixedDepositDetailDto(
    FixedDepositDto FixedDeposit,
    List<FixedDepositInterestAccrualDto> InterestAccruals,
    List<FixedDepositInterestReceiptDto> InterestReceipts);
