using AwesomeAssertions;
using MyCondo.Domain.Features.Finance.FinancialYears;
using MyCondo.Domain.Features.Finance.FinancialYears.Exceptions;

namespace MyCondo.Domain.UnitTests.Features.Finance.FinancialYears;

public class FinancialYearTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly DateOnly Start = new(2026, 1, 1);
    private static readonly DateOnly End = new(2026, 12, 31);

    [Fact]
    public void Create_Starts_Open()
    {
        FinancialYear year = FinancialYear.Create(TenantId, "FY2026", Start, End);

        year.Status.Should().Be(FinancialYearStatus.Open);
    }

    [Fact]
    public void Create_Throws_When_EndDate_Is_Not_After_StartDate()
    {
        Action act = () => FinancialYear.Create(TenantId, "FY2026", End, Start);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Close_Then_Close_Again_Throws()
    {
        FinancialYear year = FinancialYear.Create(TenantId, "FY2026", Start, End);
        year.Close();

        Action act = () => year.Close();

        act.Should().Throw<FinancialYearAlreadyClosedException>();
    }

    [Fact]
    public void Reopen_After_Close_Restores_Open_Status()
    {
        FinancialYear year = FinancialYear.Create(TenantId, "FY2026", Start, End);
        year.Close();

        year.Reopen();

        year.Status.Should().Be(FinancialYearStatus.Open);
    }
}
