using AwesomeAssertions;
using MyCondo.Domain.Features.Finance.AccountingPeriods;
using MyCondo.Domain.Features.Finance.AccountingPeriods.Exceptions;
using MyCondo.Domain.Features.Finance.FinancialYears;

namespace MyCondo.Domain.UnitTests.Features.Finance.AccountingPeriods;

public class AccountingPeriodTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly FinancialYearId FinancialYearId = FinancialYearId.New();
    private static readonly DateOnly Start = new(2026, 3, 1);
    private static readonly DateOnly End = new(2026, 3, 31);

    [Fact]
    public void Covers_Is_True_For_A_Date_Inside_The_Range_Inclusive()
    {
        AccountingPeriod period = AccountingPeriod.Create(TenantId, FinancialYearId, "2026-03", Start, End);

        period.Covers(Start).Should().BeTrue();
        period.Covers(End).Should().BeTrue();
        period.Covers(new DateOnly(2026, 3, 15)).Should().BeTrue();
        period.Covers(new DateOnly(2026, 4, 1)).Should().BeFalse();
        period.Covers(new DateOnly(2026, 2, 28)).Should().BeFalse();
    }

    [Fact]
    public void Create_Starts_Open()
    {
        AccountingPeriod period = AccountingPeriod.Create(TenantId, FinancialYearId, "2026-03", Start, End);

        period.Status.Should().Be(AccountingPeriodStatus.Open);
    }

    [Fact]
    public void Close_Then_Close_Again_Throws()
    {
        AccountingPeriod period = AccountingPeriod.Create(TenantId, FinancialYearId, "2026-03", Start, End);
        period.Close();

        Action act = () => period.Close();

        act.Should().Throw<AccountingPeriodAlreadyClosedException>();
    }
}
