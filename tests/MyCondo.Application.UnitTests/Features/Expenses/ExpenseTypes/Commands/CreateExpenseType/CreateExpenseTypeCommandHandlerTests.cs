using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Expenses.ExpenseTypes.Commands.CreateExpenseType;
using MyCondo.Application.Features.Expenses.ExpenseTypes.DTOs;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Expenses.ExpenseCategories;
using MyCondo.Domain.Features.Expenses.ExpenseTypes;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Expenses.ExpenseTypes.Commands.CreateExpenseType;

public class CreateExpenseTypeCommandHandlerTests
{
    private static readonly DateTimeOffset NowUtc = new(2026, 8, 11, 0, 0, 0, TimeSpan.Zero);
    private static readonly Guid TenantId = Guid.NewGuid();

    private readonly IExpenseTypeRepository _expenseTypes = Substitute.For<IExpenseTypeRepository>();
    private readonly IExpenseCategoryRepository _expenseCategories = Substitute.For<IExpenseCategoryRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUserProvider _currentUser = Substitute.For<ICurrentUserProvider>();
    private readonly IClock _clock = Substitute.For<IClock>();
    private readonly ExpenseCategory _category;

    public CreateExpenseTypeCommandHandlerTests()
    {
        _currentUser.TenantId.Returns(TenantId);
        _clock.UtcNow.Returns(NowUtc);
        _expenseTypes.ExistsByCodeAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<ExpenseTypeId?>(), Arg.Any<CancellationToken>())
            .Returns(false);
        _expenseTypes.ExistsByNameAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<ExpenseTypeId?>(), Arg.Any<CancellationToken>())
            .Returns(false);

        _category = ExpenseCategory.Create(TenantId, "Maintenance", "MAINT", null, 1, NowUtc);
        _expenseCategories.GetByIdAsync(_category.Id, Arg.Any<CancellationToken>()).Returns(_category);
    }

    private CreateExpenseTypeCommandHandler CreateHandler() => new(
        _expenseTypes, _expenseCategories, _unitOfWork, _currentUser, _clock,
        Substitute.For<ILogger<CreateExpenseTypeCommandHandler>>());

    [Fact]
    public async Task Creates_An_Active_Expense_Type_With_An_Uppercased_Code()
    {
        CreateExpenseTypeCommand command = new(_category.Id.Value, "Cleaning", "clean", "Cleaning services", 1);

        ExpenseTypeDto result = await CreateHandler().Handle(command, CancellationToken.None);

        result.Code.Should().Be("CLEAN");
        result.IsActive.Should().BeTrue();
        result.ExpenseCategoryId.Should().Be(_category.Id.Value);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Throws_Conflict_When_Code_Already_Exists_For_Tenant()
    {
        _expenseTypes.ExistsByCodeAsync(TenantId, "CLEAN", null, Arg.Any<CancellationToken>()).Returns(true);

        CreateExpenseTypeCommand command = new(_category.Id.Value, "Cleaning", "clean", null, 1);

        Func<Task> act = async () => await CreateHandler().Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
        _expenseTypes.DidNotReceive().Add(Arg.Any<ExpenseType>());
    }

    [Fact]
    public async Task Throws_Conflict_When_Name_Already_Exists_For_Tenant()
    {
        _expenseTypes.ExistsByNameAsync(TenantId, "Cleaning", null, Arg.Any<CancellationToken>()).Returns(true);

        CreateExpenseTypeCommand command = new(_category.Id.Value, "Cleaning", "CLN", null, 1);

        Func<Task> act = async () => await CreateHandler().Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Throws_NotFound_When_Category_Does_Not_Exist()
    {
        CreateExpenseTypeCommand command = new(Guid.NewGuid(), "Cleaning", "CLN", null, 1);

        Func<Task> act = async () => await CreateHandler().Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Throws_Conflict_When_Category_Is_Inactive()
    {
        ExpenseCategory inactive = ExpenseCategory.Create(TenantId, "Old", "OLD", null, 1, NowUtc);
        inactive.Deactivate(NowUtc);
        _expenseCategories.GetByIdAsync(inactive.Id, Arg.Any<CancellationToken>()).Returns(inactive);

        CreateExpenseTypeCommand command = new(inactive.Id.Value, "Cleaning", "CLN", null, 1);

        Func<Task> act = async () => await CreateHandler().Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }
}
