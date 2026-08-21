using AwesomeAssertions;
using MyCondo.Application.Common.Abstractions;
using MyCondo.Application.Common.Exceptions;
using MyCondo.Application.Features.Finance.Reports.Queries.GetExpenseByCategoryReport;
using MyCondo.Domain.Abstractions;
using MyCondo.Domain.Features.Expenses.Expenses;
using MyCondo.Domain.Features.Expenses.ExpenseCategories;
using NSubstitute;

namespace MyCondo.Application.UnitTests.Features.Finance.Reports.Queries.GetExpenseByCategoryReport;

public class GetExpenseByCategoryReportQueryHandlerTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 8, 15, 3, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly From = new(2026, 8, 1);
    private static readonly DateOnly To = new(2026, 8, 31);

    private readonly IExpenseRepository _expenses = Substitute.For<IExpenseRepository>();
    private readonly ICurrentUserProvider _currentUser = Substitute.For<ICurrentUserProvider>();
    private readonly IClock _clock = Substitute.For<IClock>();

    public GetExpenseByCategoryReportQueryHandlerTests()
    {
        _currentUser.TenantId.Returns(TenantId);
        _clock.UtcNow.Returns(Now);
    }

    private GetExpenseByCategoryReportQueryHandler CreateHandler() => new(_expenses, _currentUser, _clock);

    [Fact]
    public async Task Report_Total_Equals_Sum_Of_Category_Lines_Including_Uncategorized()
    {
        ExpenseCategoryId categoryId = new(Guid.NewGuid());
        _expenses.GetExpenseCompositionByCategoryAsync(TenantId, From, To, Arg.Any<CancellationToken>())
            .Returns(
            [
                new ExpenseCategoryActivityLine(categoryId, "Utilities", 6_000m),
                new ExpenseCategoryActivityLine(null, "Uncategorized", 1_500m),
            ]);

        ExpenseByCategoryReportDto result = await CreateHandler().Handle(
            new GetExpenseByCategoryReportQuery(From, To), CancellationToken.None);

        result.Lines.Should().HaveCount(2);
        result.Total.Should().Be(7_500m);
        result.Lines.Should().Contain(l => l.ExpenseCategoryId == null && l.CategoryName == "Uncategorized" && l.TotalAmount == 1_500m);
    }

    [Fact]
    public async Task Unauthenticated_Caller_Is_Rejected()
    {
        _currentUser.TenantId.Returns((Guid?)null);

        Func<Task> act = () => CreateHandler().Handle(
            new GetExpenseByCategoryReportQuery(From, To), CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<ForbiddenException>();
    }
}
