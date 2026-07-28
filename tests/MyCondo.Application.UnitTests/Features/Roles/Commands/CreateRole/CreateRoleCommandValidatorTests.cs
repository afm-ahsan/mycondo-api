using AwesomeAssertions;
using FluentValidation.Results;
using MyCondo.Application.Features.Roles.Commands.CreateRole;

namespace MyCondo.Application.UnitTests.Features.Roles.Commands.CreateRole;

public class CreateRoleCommandValidatorTests
{
    private readonly CreateRoleCommandValidator _validator = new();

    [Fact]
    public void Valid_Command_Passes()
    {
        CreateRoleCommand command = new("Building Manager", "Manages day-to-day building operations.");

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_Name_Fails()
    {
        CreateRoleCommand command = new("", "Description");

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateRoleCommand.Name));
    }

    [Fact]
    public void Name_Over_MaxLength_Fails()
    {
        CreateRoleCommand command = new(new string('a', 81), "Description");

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateRoleCommand.Name));
    }

    [Fact]
    public void Description_Over_MaxLength_Fails()
    {
        CreateRoleCommand command = new("Building Manager", new string('a', 401));

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateRoleCommand.Description));
    }
}
