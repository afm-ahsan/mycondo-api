using AwesomeAssertions;
using MyCondo.Domain.Features.Platform.SubscriptionPackages;
using MyCondo.Domain.Features.Platform.SubscriptionPackages.Exceptions;

namespace MyCondo.Domain.UnitTests.Features.Platform.SubscriptionPackages;

public class SubscriptionPackageTests
{
    [Fact]
    public void Create_Sets_Code_Name_Description_And_Starts_Draft_With_No_Current_Version()
    {
        SubscriptionPackage package = SubscriptionPackage.Create("professional", "Professional", "For growing associations");

        package.Code.Should().Be("professional");
        package.Name.Should().Be("Professional");
        package.Description.Should().Be("For growing associations");
        package.Status.Should().Be(SubscriptionPackageStatus.Draft);
        package.CurrentVersionId.Should().BeNull();
    }

    [Fact]
    public void Create_Trims_Code_Name_And_Description()
    {
        SubscriptionPackage package = SubscriptionPackage.Create("  professional  ", "  Professional  ", "  desc  ");

        package.Code.Should().Be("professional");
        package.Name.Should().Be("Professional");
        package.Description.Should().Be("desc");
    }

    [Fact]
    public void Create_Throws_For_Blank_Code()
    {
        Action act = () => SubscriptionPackage.Create("  ", "Professional", null);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_Throws_For_Blank_Name()
    {
        Action act = () => SubscriptionPackage.Create("professional", "  ", null);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Activate_Draft_Transitions_To_Active()
    {
        SubscriptionPackage package = SubscriptionPackage.Create("professional", "Professional", null);

        package.Activate();

        package.Status.Should().Be(SubscriptionPackageStatus.Active);
    }

    [Fact]
    public void Activate_Already_Active_Throws()
    {
        SubscriptionPackage package = SubscriptionPackage.Create("professional", "Professional", null);
        package.Activate();

        Action act = () => package.Activate();

        act.Should().Throw<SubscriptionPackageInvalidTransitionException>();
    }

    [Fact]
    public void Retire_From_Draft_Transitions_To_Retired()
    {
        SubscriptionPackage package = SubscriptionPackage.Create("professional", "Professional", null);

        package.Retire();

        package.Status.Should().Be(SubscriptionPackageStatus.Retired);
    }

    [Fact]
    public void Retire_From_Active_Transitions_To_Retired()
    {
        SubscriptionPackage package = SubscriptionPackage.Create("professional", "Professional", null);
        package.Activate();

        package.Retire();

        package.Status.Should().Be(SubscriptionPackageStatus.Retired);
    }

    [Fact]
    public void Retire_Already_Retired_Throws()
    {
        SubscriptionPackage package = SubscriptionPackage.Create("professional", "Professional", null);
        package.Retire();

        Action act = () => package.Retire();

        act.Should().Throw<SubscriptionPackageInvalidTransitionException>();
    }

    [Fact]
    public void SetCurrentVersion_On_Active_Package_Sets_Pointer()
    {
        SubscriptionPackage package = SubscriptionPackage.Create("professional", "Professional", null);
        package.Activate();
        SubscriptionPackageVersionId versionId = SubscriptionPackageVersionId.New();

        package.SetCurrentVersion(versionId);

        package.CurrentVersionId.Should().Be(versionId);
    }

    [Fact]
    public void SetCurrentVersion_On_Retired_Package_Throws()
    {
        SubscriptionPackage package = SubscriptionPackage.Create("professional", "Professional", null);
        package.Retire();

        Action act = () => package.SetCurrentVersion(SubscriptionPackageVersionId.New());

        act.Should().Throw<SubscriptionPackageRetiredException>();
    }

    [Fact]
    public void Two_Packages_With_Different_Ids_Are_Not_Equal()
    {
        SubscriptionPackage a = SubscriptionPackage.Create("professional", "Professional", null);
        SubscriptionPackage b = SubscriptionPackage.Create("professional", "Professional", null);

        a.Should().NotBe(b);
        a.Id.Should().NotBe(b.Id);
    }
}
