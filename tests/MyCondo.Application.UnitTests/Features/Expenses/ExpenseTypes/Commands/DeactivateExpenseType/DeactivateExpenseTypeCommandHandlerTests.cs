using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Expenses.ExpenseTypes.Commands.DeactivateExpenseType;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Expenses.ExpenseTypes;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Expenses.ExpenseTypes.Commands.DeactivateExpenseType;

public class DeactivateExpenseTypeCommandHandlerTests
{
    private static readonly DateTimeOffset NowUtc = new(2026, 8, 11, 0, 0, 0, TimeSpan.Zero);
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid OtherTenantId = Guid.NewGuid();

    private readonly IExpenseTypeRepository _expenseTypes = Substitute.For<IExpenseTypeRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUserProvider _currentUser = Substitute.For<ICurrentUserProvider>();
    private readonly IClock _clock = Substitute.For<IClock>();

    public DeactivateExpenseTypeCommandHandlerTests()
    {
        _currentUser.TenantId.Returns(TenantId);
        _clock.UtcNow.Returns(NowUtc);
    }

    private DeactivateExpenseTypeCommandHandler CreateHandler() => new(
        _expenseTypes, _unitOfWork, _currentUser, _clock, Substitute.For<ILogger<DeactivateExpenseTypeCommandHandler>>());

    private static ExpenseType CreateType(Guid tenantId) =>
        ExpenseType.Create(tenantId, "Cleaning", "CLN", null, 1, NowUtc);

    [Fact]
    public async Task Deactivates_An_Expense_Type_In_Callers_Tenant()
    {
        ExpenseType expenseType = CreateType(TenantId);
        _expenseTypes.GetByIdAsync(expenseType.Id, Arg.Any<CancellationToken>()).Returns(expenseType);

        await CreateHandler().Handle(new DeactivateExpenseTypeCommand(expenseType.Id.Value), CancellationToken.None);

        expenseType.IsActive.Should().BeFalse();
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Throws_NotFound_When_Expense_Type_Belongs_To_A_Different_Tenant()
    {
        ExpenseType expenseType = CreateType(OtherTenantId);
        _expenseTypes.GetByIdAsync(expenseType.Id, Arg.Any<CancellationToken>()).Returns(expenseType);

        Func<Task> act = async () => await CreateHandler()
            .Handle(new DeactivateExpenseTypeCommand(expenseType.Id.Value), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
