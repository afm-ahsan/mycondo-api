using AwesomeAssertions;
using MyCondo.Domain.Features.Finance.AccountMappings;
using MyCondo.Domain.Features.Finance.ChartOfAccounts;

namespace MyCondo.Domain.UnitTests.Features.Finance.AccountMappings;

public class AccountMappingTests
{
    private static readonly Guid TenantId = Guid.NewGuid();

    [Fact]
    public void Create_Maps_A_Posting_Role_To_An_Account()
    {
        ChartOfAccountId accountId = ChartOfAccountId.New();

        AccountMapping mapping = AccountMapping.Create(TenantId, "CashOrBank", accountId);

        mapping.TenantId.Should().Be(TenantId);
        mapping.PostingRole.Should().Be("CashOrBank");
        mapping.ChartOfAccountId.Should().Be(accountId);
    }

    [Fact]
    public void Remap_Points_The_Role_At_A_Different_Account()
    {
        AccountMapping mapping = AccountMapping.Create(TenantId, "CashOrBank", ChartOfAccountId.New());
        ChartOfAccountId newAccountId = ChartOfAccountId.New();

        mapping.Remap(newAccountId);

        mapping.ChartOfAccountId.Should().Be(newAccountId);
    }

    [Fact]
    public void Create_Throws_When_PostingRole_Is_Blank()
    {
        Action act = () => AccountMapping.Create(TenantId, "  ", ChartOfAccountId.New());

        act.Should().Throw<ArgumentException>();
    }
}
