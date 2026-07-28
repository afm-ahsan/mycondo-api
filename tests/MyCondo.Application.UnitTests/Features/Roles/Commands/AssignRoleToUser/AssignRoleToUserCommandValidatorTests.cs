using AwesomeAssertions;
using FluentValidation.Results;
using MyCondo.Application.Features.Roles.Commands.AssignRoleToUser;

namespace MyCondo.Application.UnitTests.Features.Roles.Commands.AssignRoleToUser;

public class AssignRoleToUserCommandValidatorTests
{
    private readonly AssignRoleToUserCommandValidator _validator = new();

    [Fact]
    public void Valid_Command_Without_BuildingId_Passes()
    {
        AssignRoleToUserCommand command = new(Guid.NewGuid(), Guid.NewGuid(), null);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Valid_Command_With_BuildingId_Passes()
    {
        AssignRoleToUserCommand command = new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_RoleId_Fails()
    {
        AssignRoleToUserCommand command = new(Guid.Empty, Guid.NewGuid(), null);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(AssignRoleToUserCommand.RoleId));
    }

    [Fact]
    public void Empty_UserId_Fails()
    {
        AssignRoleToUserCommand command = new(Guid.NewGuid(), Guid.Empty, null);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(AssignRoleToUserCommand.UserId));
    }
}
