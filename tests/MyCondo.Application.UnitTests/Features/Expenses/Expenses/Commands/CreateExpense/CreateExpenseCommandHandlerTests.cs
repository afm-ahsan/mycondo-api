using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Expenses.Expenses.Commands.CreateExpense;
using MyCondo.Application.Features.Expenses.Expenses.DTOs;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Expenses.ExpenseCategories;
using MyCondo.Domain.Features.Expenses.Expenses;
using MyCondo.Domain.Features.Expenses.ExpenseTypes;
using MyCondo.Domain.Features.Finance.Funds;
using MyCondo.Domain.Features.Property.Buildings;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Expenses.Expenses.Commands.CreateExpense;

public class CreateExpenseCommandHandlerTests
{
    private static readonly DateTimeOffset NowUtc = new(2026, 8, 11, 0, 0, 0, TimeSpan.Zero);
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid OtherTenantId = Guid.NewGuid();

    private readonly IExpenseRepository _expenses = Substitute.For<IExpenseRepository>();
    private readonly IExpenseTypeRepository _expenseTypes = Substitute.For<IExpenseTypeRepository>();
    private readonly IExpenseCategoryRepository _expenseCategories = Substitute.For<IExpenseCategoryRepository>();
    private readonly IBuildingRepository _buildings = Substitute.For<IBuildingRepository>();
    private readonly IFundRepository _funds = Substitute.For<IFundRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUserProvider _currentUser = Substitute.For<ICurrentUserProvider>();
    private readonly IClock _clock = Substitute.For<IClock>();

    private readonly Building _building = Building.Create(TenantId, "Tower A", "TA", null, NowUtc);
    private readonly ExpenseType _expenseType;

    public CreateExpenseCommandHandlerTests()
    {
        _expenseType = ExpenseType.Create(TenantId, ExpenseCategoryId.New(), "Cleaning", "CLN", null, 1, NowUtc);

        _currentUser.TenantId.Returns(TenantId);
        _currentUser.HasPermissionForBuilding(Arg.Any<string>(), Arg.Any<Guid?>()).Returns(true);
        _clock.UtcNow.Returns(NowUtc);
        _buildings.GetByIdAsync(_building.Id, Arg.Any<CancellationToken>()).Returns(_building);
        _expenseTypes.GetByIdAsync(_expenseType.Id, Arg.Any<CancellationToken>()).Returns(_expenseType);
    }

    private CreateExpenseCommandHandler CreateHandler() => new(
        _expenses, _expenseTypes, _expenseCategories, _buildings, _funds, _unitOfWork, _currentUser, _clock,
        Substitute.For<ILogger<CreateExpenseCommandHandler>>());

    private static CreateExpenseCommand ValidCommand(Guid? buildingId, Guid expenseTypeId) => new(
        buildingId, expenseTypeId, null, new DateOnly(2026, 8, 1), null, "Monthly cleaning", "ABC Cleaners",
        "INV-001", 1500m, false, "Cash", null);

    [Fact]
    public async Task Records_An_Expense_For_A_Building_The_Caller_Can_Manage()
    {
        ExpenseDto result = await CreateHandler()
            .Handle(ValidCommand(_building.Id.Value, _expenseType.Id.Value), CancellationToken.None);

        result.Amount.Should().Be(1500m);
        result.BuildingName.Should().Be("Tower A");
        result.ExpenseTypeName.Should().Be("Cleaning");
        result.Status.Should().Be("Recorded");
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Records_An_Association_Wide_Expense_With_No_Building()
    {
        ExpenseDto result = await CreateHandler()
            .Handle(ValidCommand(null, _expenseType.Id.Value), CancellationToken.None);

        result.BuildingId.Should().BeNull();
        result.BuildingName.Should().BeNull();
    }

    [Fact]
    public async Task Throws_NotFound_When_Building_Belongs_To_A_Different_Tenant()
    {
        Building otherTenantBuilding = Building.Create(OtherTenantId, "Tower B", "TB", null, NowUtc);
        _buildings.GetByIdAsync(otherTenantBuilding.Id, Arg.Any<CancellationToken>()).Returns(otherTenantBuilding);

        Func<Task> act = async () => await CreateHandler()
            .Handle(ValidCommand(otherTenantBuilding.Id.Value, _expenseType.Id.Value), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Throws_Forbidden_When_Caller_Lacks_Permission_For_The_Building()
    {
        _currentUser.HasPermissionForBuilding("expense.manage", _building.Id.Value).Returns(false);

        Func<Task> act = async () => await CreateHandler()
            .Handle(ValidCommand(_building.Id.Value, _expenseType.Id.Value), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Throws_Conflict_When_Expense_Type_Is_Inactive()
    {
        _expenseType.Deactivate(NowUtc);

        Func<Task> act = async () => await CreateHandler()
            .Handle(ValidCommand(_building.Id.Value, _expenseType.Id.Value), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }
}
