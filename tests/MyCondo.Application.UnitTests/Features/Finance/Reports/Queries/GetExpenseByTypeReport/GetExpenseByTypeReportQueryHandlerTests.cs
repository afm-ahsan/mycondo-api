using AwesomeAssertions;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Finance.Reports.Queries.GetExpenseByTypeReport;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Expenses.Expenses;
using MyCondo.Domain.Features.Expenses.ExpenseCategories;
using MyCondo.Domain.Features.Expenses.ExpenseTypes;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Finance.Reports.Queries.GetExpenseByTypeReport;

public class GetExpenseByTypeReportQueryHandlerTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 8, 15, 3, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly From = new(2026, 8, 1);
    private static readonly DateOnly To = new(2026, 8, 31);

    private readonly IExpenseRepository _expenses = Substitute.For<IExpenseRepository>();
    private readonly ICurrentUserProvider _currentUser = Substitute.For<ICurrentUserProvider>();
    private readonly IClock _clock = Substitute.For<IClock>();

    public GetExpenseByTypeReportQueryHandlerTests()
    {
        _currentUser.TenantId.Returns(TenantId);
        _clock.UtcNow.Returns(Now);
    }

    private GetExpenseByTypeReportQueryHandler CreateHandler() => new(_expenses, _currentUser, _clock);

    [Fact]
    public async Task Report_Total_Equals_Sum_Of_Type_Lines()
    {
        ExpenseTypeId typeId1 = new(Guid.NewGuid());
        ExpenseTypeId typeId2 = new(Guid.NewGuid());
        ExpenseCategoryId categoryId = new(Guid.NewGuid());

        _expenses.GetExpenseCompositionByTypeAsync(TenantId, From, To, Arg.Any<CancellationToken>())
            .Returns(
            [
                new ExpenseTypeActivityLine(typeId1, "Generator Fuel", categoryId, "Utilities", 4, 4_000m),
                new ExpenseTypeActivityLine(typeId2, "Security", categoryId, "Utilities", 2, 2_000m),
            ]);

        ExpenseByTypeReportDto result = await CreateHandler().Handle(
            new GetExpenseByTypeReportQuery(From, To), CancellationToken.None);

        result.Lines.Should().HaveCount(2);
        result.Total.Should().Be(6_000m);
        result.Lines.Should().Contain(l => l.ExpenseTypeId == typeId1.Value && l.Count == 4 && l.TotalAmount == 4_000m);
    }

    [Fact]
    public async Task Unauthenticated_Caller_Is_Rejected()
    {
        _currentUser.TenantId.Returns((Guid?)null);

        Func<Task> act = () => CreateHandler().Handle(
            new GetExpenseByTypeReportQuery(From, To), CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<ForbiddenException>();
    }
}
