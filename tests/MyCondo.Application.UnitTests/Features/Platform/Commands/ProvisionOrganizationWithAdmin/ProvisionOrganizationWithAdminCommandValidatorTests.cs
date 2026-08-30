using AwesomeAssertions;
using FluentValidation.Results;
using MyCondo.Application.Features.Platform.Commands.ProvisionOrganizationWithAdmin;
using MyCondo.Domain.Features.Platform.OrganizationSubscriptions;

namespace MyCondo.Application.UnitTests.Features.Platform.Commands.ProvisionOrganizationWithAdmin;

public class ProvisionOrganizationWithAdminCommandValidatorTests
{
    private readonly ProvisionOrganizationWithAdminCommandValidator _validator = new();

    private static ProvisionOrganizationWithAdminCommand ValidCommand() => new(
        Name: "Akter Residence Park",
        Code: "ARP",
        Slug: "arp",
        AdministratorFullName: "Admin",
        AdministratorEmail: "admin@mycondo.com",
        AdministratorPassword: "Correct-Horse-Battery-9",
        EnabledModuleKeys: ["billing", "payments"],
        SubscriptionPackageVersionId: Guid.NewGuid(),
        BillingCycle: BillingCycle.Monthly,
        AutoRenew: true);

    [Fact]
    public void Valid_Command_Passes()
    {
        ValidationResult result = _validator.Validate(ValidCommand());

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("arp")]
    [InlineData("A R P")]
    [InlineData("-ARP")]
    public void Rejects_Malformed_Code(string code)
    {
        ProvisionOrganizationWithAdminCommand command = ValidCommand() with { Code = code };

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(ProvisionOrganizationWithAdminCommand.Code));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    public void Rejects_Invalid_AdministratorEmail(string email)
    {
        ProvisionOrganizationWithAdminCommand command = ValidCommand() with { AdministratorEmail = email };

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(
            e => e.PropertyName == nameof(ProvisionOrganizationWithAdminCommand.AdministratorEmail));
    }

    [Theory]
    [InlineData("Sh0!")]
    [InlineData("NoSpecialChar12")]
    public void Rejects_Weak_Password(string weakPassword)
    {
        ProvisionOrganizationWithAdminCommand command = ValidCommand() with { AdministratorPassword = weakPassword };

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(
            e => e.PropertyName == nameof(ProvisionOrganizationWithAdminCommand.AdministratorPassword));
    }

    [Fact]
    public void Rejects_Unknown_Module_Key()
    {
        ProvisionOrganizationWithAdminCommand command = ValidCommand() with { EnabledModuleKeys = ["not-a-real-module"] };

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(
            e => e.PropertyName == nameof(ProvisionOrganizationWithAdminCommand.EnabledModuleKeys));
    }

    [Fact]
    public void Rejects_Empty_SubscriptionPackageVersionId()
    {
        ProvisionOrganizationWithAdminCommand command = ValidCommand() with { SubscriptionPackageVersionId = Guid.Empty };

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(
            e => e.PropertyName == nameof(ProvisionOrganizationWithAdminCommand.SubscriptionPackageVersionId));
    }

    [Fact]
    public void Rejects_Undefined_BillingCycle()
    {
        ProvisionOrganizationWithAdminCommand command = ValidCommand() with { BillingCycle = (BillingCycle)999 };

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(
            e => e.PropertyName == nameof(ProvisionOrganizationWithAdminCommand.BillingCycle));
    }
}
