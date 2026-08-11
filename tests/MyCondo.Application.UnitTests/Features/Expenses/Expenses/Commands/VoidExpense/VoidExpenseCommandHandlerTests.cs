using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Expenses.Expenses.Commands.VoidExpense;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Expenses.Expenses;
using MyCondo.Domain.Features.Expenses.ExpenseTypes;
using MyCondo.Domain.Features.Payments.Payments;
using MyCondo.Domain.Features.Property.Buildings;
using MyCondo.Domain.Features.Property.Flats;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Expenses.Expenses.Commands.VoidExpense;

public class VoidExpenseCommandHandlerTests
{
    private static readonly DateTimeOffset NowUtc = new(2026, 8, 11, 0, 0, 0, TimeSpan.Zero);
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid OtherTenantId = Guid.NewGuid();
    private static readonly BuildingId ABuildingId = new(Guid.NewGuid());

    private readonly IExpenseRepository _expenses = Substitute.For<IExpenseRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUserProvider _currentUser = Substitute.For<ICurrentUserProvider>();
    private readonly IClock _clock = Substitute.For<IClock>();

    public VoidExpenseCommandHandlerTests()
    {
        _currentUser.TenantId.Returns(TenantId);
        _currentUser.HasPermissionForBuilding(Arg.Any<string>(), Arg.Any<Guid?>()).Returns(true);
        _clock.UtcNow.Returns(NowUtc);
    }

    private VoidExpenseCommandHandler CreateHandler() => new(
        _expenses, _unitOfWork, _currentUser, _clock, Substitute.For<ILogger<VoidExpenseCommandHandler>>());

    private static Expense RecordExpense(Guid tenantId) => Expense.Record(
        tenantId, ABuildingId, new ExpenseTypeId(Guid.NewGuid()), new DateOnly(2026, 8, 1), "Cleaning", null, null,
        1000m, PaymentMethod.Cash, null, NowUtc);

    [Fact]
    public async Task Voids_An_Expense_With_A_Reason()
    {
        Expense expense = RecordExpense(TenantId);
        _expenses.GetByIdAsync(expense.Id, Arg.Any<CancellationToken>()).Returns(expense);

        await CreateHandler().Handle(new VoidExpenseCommand(expense.Id.Value, "Recorded in error"), CancellationToken.None);

        expense.Status.Should().Be(ExpenseStatus.Voided);
        expense.VoidReason.Should().Be("Recorded in error");
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Throws_NotFound_When_Expense_Belongs_To_A_Different_Tenant()
    {
        Expense expense = RecordExpense(OtherTenantId);
        _expenses.GetByIdAsync(expense.Id, Arg.Any<CancellationToken>()).Returns(expense);

        Func<Task> act = async () => await CreateHandler()
            .Handle(new VoidExpenseCommand(expense.Id.Value, "Reason"), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Throws_Forbidden_When_Caller_Lacks_Permission_For_The_Buildings_Scope()
    {
        Expense expense = RecordExpense(TenantId);
        _expenses.GetByIdAsync(expense.Id, Arg.Any<CancellationToken>()).Returns(expense);
        _currentUser.HasPermissionForBuilding("expense.manage", ABuildingId.Value).Returns(false);

        Func<Task> act = async () => await CreateHandler()
            .Handle(new VoidExpenseCommand(expense.Id.Value, "Reason"), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }
}
