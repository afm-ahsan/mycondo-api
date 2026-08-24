using MyCondo.Domain.Exceptions;

namespace MyCondo.Domain.Features.Finance.ChartOfAccounts.Exceptions;

/// <summary>Thrown when a <see cref="ChartOfAccount"/> is created or reclassified with a
/// <see cref="FinancialStatementGroup"/> that belongs to a different <see cref="AccountCategory"/> than
/// the account's own — e.g. assigning the Asset-only <see cref="FinancialStatementGroup.CashAndBank"/>
/// group to a Liability account would misstate the Statement of Financial Position.</summary>
public sealed class StatementGroupCategoryMismatchException(
    AccountCategory accountCategory, FinancialStatementGroup statementGroup)
    : DomainException(
        $"Financial statement group '{statementGroup}' belongs to category " +
        $"'{FinancialStatementGroupCategories.CategoryOf(statementGroup)}', not '{accountCategory}'.");
