using AwesomeAssertions;
using FluentValidation.Results;
using MyCondo.Application.Features.Tenancy.Commands.ProvisionTenant;

namespace MyCondo.Application.UnitTests.Features.Tenancy.Commands.ProvisionTenant;

public class ProvisionTenantCommandValidatorTests
{
    private readonly ProvisionTenantCommandValidator _validator = new();

    [Fact]
    public void Valid_Command_Passes()
    {
        ProvisionTenantCommand command = new("ARP Flat Owners", "arp-flat-owners");

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_Name_Fails()
    {
        ProvisionTenantCommand command = new("", "arp");

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(ProvisionTenantCommand.Name));
    }

    [Theory]
    [InlineData("")]
    [InlineData("ARP")]
    [InlineData("arp_flat")]
    [InlineData("-arp")]
    [InlineData("arp-")]
    [InlineData("arp--flat")]
    public void Invalid_Slug_Shape_Fails(string slug)
    {
        ProvisionTenantCommand command = new("ARP", slug);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(ProvisionTenantCommand.Slug));
    }

    [Theory]
    [InlineData("arp")]
    [InlineData("arp-flat-owners")]
    [InlineData("a1b2c3")]
    public void Valid_Slug_Shapes_Pass(string slug)
    {
        ProvisionTenantCommand command = new("ARP", slug);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }
}
