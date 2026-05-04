using System.Globalization;

namespace SharedData;

public enum ExpenseSortOption
{
    NewestFirst,
    OldestFirst,
    HighestAmount,
    LowestAmount
}

public sealed class ExpenseQueryOptions
{
    public int? CategoryId { get; init; }
    public ExpenseSortOption SortOption { get; init; } = ExpenseSortOption.NewestFirst;
}

public sealed class CategoryExpenseSummary
{
    public int CategoryId { get; init; }
    public string CategoryName { get; init; } = "";
    public double TotalExpenses { get; init; }
    public double MonthlyAverage { get; init; }
    public int ExpenseCount { get; init; }
}

public sealed class ExpenseInsightsSnapshot
{
    public IReadOnlyList<CategoryExpenseSummary> CategorySummaries { get; init; } = Array.Empty<CategoryExpenseSummary>();
    public IReadOnlyList<Expense> FilteredExpenses { get; init; } = Array.Empty<Expense>();
    public string ScopeLabel { get; init; } = "All categories";
    public string SortLabel { get; init; } = "Newest first";
}

public delegate void ExpenseInsightsUpdatedHandler(ExpenseInsightsSnapshot snapshot);

public static class ExpenseInsightsService
{
    // Groups expenses by category and prepares the summary cards on the dashboard.
    public static IReadOnlyList<CategoryExpenseSummary> BuildCategorySummaries(
        IEnumerable<Category> categories,
        IEnumerable<Expense> expenses)
    {
        return categories
            .GroupJoin(
                expenses,
                category => category.CategoryId,
                expense => expense.CategoryId,
                (category, categoryExpenses) =>
                {
                    var expenseList = categoryExpenses.ToList();
                    return new CategoryExpenseSummary
                    {
                        CategoryId = category.CategoryId,
                        CategoryName = category.Name,
                        TotalExpenses = expenseList.Sum(expense => expense.Amount),
                        MonthlyAverage = CalculateMonthlyAverage(expenseList),
                        ExpenseCount = expenseList.Count
                    };
                })
            .OrderByDescending(summary => summary.TotalExpenses)
            .ThenBy(summary => summary.CategoryName)
            .ToList();
    }

    // Filters and sorts the personal expense list in real time with LINQ.
    public static IReadOnlyList<Expense> FilterAndSortExpenses(
        IEnumerable<Expense> expenses,
        ExpenseQueryOptions options)
    {
        var query = expenses;

        if (options.CategoryId.HasValue)
        {
            query = query.Where(expense => expense.CategoryId == options.CategoryId.Value);
        }

        return options.SortOption switch
        {
            ExpenseSortOption.OldestFirst => query
                .OrderBy(TryParseExpenseDate)
                .ThenBy(expense => expense.Description)
                .ToList(),
            ExpenseSortOption.HighestAmount => query
                .OrderByDescending(expense => expense.Amount)
                .ThenByDescending(TryParseExpenseDate)
                .ToList(),
            ExpenseSortOption.LowestAmount => query
                .OrderBy(expense => expense.Amount)
                .ThenByDescending(TryParseExpenseDate)
                .ToList(),
            _ => query
                .OrderByDescending(TryParseExpenseDate)
                .ThenBy(expense => expense.Description)
                .ToList()
        };
    }

    // Builds the filtered report and pushes it to the UI through a delegate.
    public static void NotifyInsightsUpdated(
        IEnumerable<Category> categories,
        IEnumerable<Expense> expenses,
        ExpenseQueryOptions options,
        string scopeLabel,
        string sortLabel,
        ExpenseInsightsUpdatedHandler onInsightsUpdated)
    {
        var snapshot = new ExpenseInsightsSnapshot
        {
            CategorySummaries = BuildCategorySummaries(categories, expenses),
            FilteredExpenses = FilterAndSortExpenses(expenses, options),
            ScopeLabel = scopeLabel,
            SortLabel = sortLabel
        };

        onInsightsUpdated(snapshot);
    }

    // Calculates the average spent per month by grouping stored expense rows.
    public static double CalculateMonthlyAverage(IEnumerable<Expense> expenses)
    {
        return expenses
            .GroupBy(GetExpenseMonthKey)
            .Select(month => month.Sum(expense => expense.Amount))
            .DefaultIfEmpty(0)
            .Average();
    }

    public static bool IsInCurrentMonth(Expense expense)
    {
        var date = TryParseExpenseDate(expense);
        return date.Year == DateTime.Now.Year &&
               date.Month == DateTime.Now.Month;
    }

    private static string GetExpenseMonthKey(Expense expense)
    {
        var date = TryParseExpenseDate(expense);
        return date == DateTime.MinValue
            ? expense.Date
            : date.ToString("yyyy-MM", CultureInfo.InvariantCulture);
    }

    private static DateTime TryParseExpenseDate(Expense expense)
    {
        return DateTime.TryParse(
            expense.Date,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var parsedDate)
            ? parsedDate
            : DateTime.MinValue;
    }
}
