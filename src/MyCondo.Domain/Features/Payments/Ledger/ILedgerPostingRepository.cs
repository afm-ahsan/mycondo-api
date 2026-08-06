namespace MyCondo.Domain.Features.Payments.Ledger;

public interface ILedgerPostingRepository
{
    void Add(LedgerPosting posting);
}
