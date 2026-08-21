namespace MyCondo.Domain.Features.Finance.FinancialAccounts;

/// <summary>Current MyCondo equivalents of the physical money locations Template 4 requires — Cash on
/// hand, a Bank account (the common case), and a Mobile Financial Service wallet (e.g. bKash/Nagad).
/// </summary>
public enum FinancialAccountType
{
    Cash = 0,
    Bank = 1,
    MobileFinancialService = 2,
}
