using AwesomeAssertions;
using FluentValidation.Results;
using MyCondo.Application.Features.Roles.Commands.GrantPermissionToRole;

namespace MyCondo.Application.UnitTests.Features.Roles.Commands.GrantPermissionToRole;

public class GrantPermissionToRoleCommandValidatorTests
{
    private readonly GrantPermissionToRoleCommandValidator _validator = new();

    [Fact]
    public void Valid_Command_Passes()
    {
        GrantPermissionToRoleCommand command = new(Guid.NewGuid(), Guid.NewGuid());

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_RoleId_Fails()
    {
        GrantPermissionToRoleCommand command = new(Guid.Empty, Guid.NewGuid());

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(GrantPermissionToRoleCommand.RoleId));
    }

    [Fact]
    public void Empty_PermissionId_Fails()
    {
        GrantPermissionToRoleCommand command = new(Guid.NewGuid(), Guid.Empty);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(GrantPermissionToRoleCommand.PermissionId));
    }
}
