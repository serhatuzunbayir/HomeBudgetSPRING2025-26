using SharedData;
using Xunit;

namespace SharedData.Tests;

public sealed class SharedDataServiceTests
{
    [Fact]
    public void GetActiveAlerts_ReturnsOnlyAlertingCategoriesInPriorityOrder()
    {
        var categories = new[]
        {
            new Category { CategoryId = 1, Name = "Food", Budget = 25, WarningThreshold = 50 },
            new Category { CategoryId = 2, Name = "Rent", Budget = -10, WarningThreshold = 100 },
            new Category { CategoryId = 3, Name = "Utilities", Budget = 200, WarningThreshold = 50 },
            new Category { CategoryId = 4, Name = "Transport", Budget = 5, WarningThreshold = 25 }
        };

        var alerts = BudgetAlertService.GetActiveAlerts(categories);

        Assert.Equal(new[] { 2, 4, 1 }, alerts.Select(alert => alert.CategoryId));
    }

    [Fact]
    public void EvaluateThresholdCrossing_ReturnsNotificationWhenCategoryNewlyFallsBelowThreshold()
    {
        var previous = new Category { CategoryId = 7, Name = "Groceries", Budget = 75, WarningThreshold = 50 };
        var current = new Category { CategoryId = 7, Name = "Groceries", Budget = 40, WarningThreshold = 50 };

        var notification = BudgetAlertService.EvaluateThresholdCrossing(previous, current);

        Assert.NotNull(notification);
        Assert.Equal("Groceries", notification.CategoryName);
        Assert.Equal(40, notification.RemainingBudget);
        Assert.Equal(50, notification.WarningThreshold);
        Assert.False(notification.IsOverBudget);
    }

    [Fact]
    public void EvaluateThresholdCrossing_ReturnsNullWhenCategoryWasAlreadyAlerting()
    {
        var previous = new Category { CategoryId = 8, Name = "Dining", Budget = 20, WarningThreshold = 50 };
        var current = new Category { CategoryId = 8, Name = "Dining", Budget = 10, WarningThreshold = 50 };

        var notification = BudgetAlertService.EvaluateThresholdCrossing(previous, current);

        Assert.Null(notification);
    }

    [Fact]
    public async Task NotifyIfThresholdCrossedAsync_CallsDelegateWithNotification()
    {
        var previous = new Category { CategoryId = 11, Name = "Travel", Budget = 150, WarningThreshold = 100 };
        var current = new Category { CategoryId = 11, Name = "Travel", Budget = -5, WarningThreshold = 100 };
        BudgetAlertNotification? capturedNotification = null;

        await BudgetAlertService.NotifyIfThresholdCrossedAsync(
            previous,
            current,
            notification =>
            {
                capturedNotification = notification;
                return Task.CompletedTask;
            });

        Assert.NotNull(capturedNotification);
        Assert.True(capturedNotification.IsOverBudget);
        Assert.Equal("Budget exceeded", capturedNotification.Title);
    }

    [Fact]
    public void BuildCategorySummaries_IncludesCategoriesWithoutExpenses()
    {
        var categories = new[]
        {
            new Category { CategoryId = 1, Name = "Food" },
            new Category { CategoryId = 2, Name = "Transport" }
        };
        var expenses = new[]
        {
            new Expense { CategoryId = 1, Description = "Market", Amount = 45, Date = "2026-05-01" },
            new Expense { CategoryId = 1, Description = "Bakery", Amount = 15, Date = "2026-05-02" }
        };

        var summaries = ExpenseInsightsService.BuildCategorySummaries(categories, expenses);

        Assert.Equal(2, summaries.Count);
        Assert.Equal("Food", summaries[0].CategoryName);
        Assert.Equal(60, summaries[0].TotalExpenses);
        Assert.Equal(2, summaries[0].ExpenseCount);
        Assert.Equal("Transport", summaries[1].CategoryName);
        Assert.Equal(0, summaries[1].TotalExpenses);
    }

    [Fact]
    public void CalculateMonthlyAverage_AveragesMonthlyTotalsInsteadOfIndividualExpenses()
    {
        var expenses = new[]
        {
            new Expense { Amount = 25, Date = "2026-04-02" },
            new Expense { Amount = 75, Date = "2026-04-20" },
            new Expense { Amount = 50, Date = "2026-05-10" }
        };

        var average = ExpenseInsightsService.CalculateMonthlyAverage(expenses);

        Assert.Equal(75, average);
    }

    [Fact]
    public void FilterAndSortExpenses_FiltersByCategoryAndSortsNewestFirstByDefault()
    {
        var expenses = new[]
        {
            new Expense { CategoryId = 1, Description = "Older", Amount = 10, Date = "2026-05-01" },
            new Expense { CategoryId = 1, Description = "Newer", Amount = 20, Date = "2026-05-03" },
            new Expense { CategoryId = 2, Description = "Ignored", Amount = 30, Date = "2026-05-04" }
        };

        var filtered = ExpenseInsightsService.FilterAndSortExpenses(
            expenses,
            new ExpenseQueryOptions { CategoryId = 1 });

        Assert.Equal(new[] { "Newer", "Older" }, filtered.Select(expense => expense.Description));
    }

    [Fact]
    public void FilterAndSortExpenses_SortsHighestAmountThenNewestDate()
    {
        var expenses = new[]
        {
            new Expense { Description = "Older high", Amount = 100, Date = "2026-05-01" },
            new Expense { Description = "Newer high", Amount = 100, Date = "2026-05-02" },
            new Expense { Description = "Low", Amount = 10, Date = "2026-05-03" }
        };

        var sorted = ExpenseInsightsService.FilterAndSortExpenses(
            expenses,
            new ExpenseQueryOptions { SortOption = ExpenseSortOption.HighestAmount });

        Assert.Equal(new[] { "Newer high", "Older high", "Low" }, sorted.Select(expense => expense.Description));
    }

    [Fact]
    public void EvaluateBudgetCapGoal_ReturnsReachedOnDeadlineWhenSpendingIsWithinCap()
    {
        var goal = new Goal
        {
            Name = "May cap",
            TargetAmount = 500,
            Deadline = "2026-05-31"
        };

        var status = SpendingGoalService.EvaluateBudgetCapGoal(goal, 500, new DateTime(2026, 5, 31));

        Assert.Equal(GoalStatus.Reached, status);
    }

    [Fact]
    public void EvaluateBudgetCapGoal_ReturnsMissedImmediatelyWhenSpendingExceedsCap()
    {
        var goal = new Goal
        {
            Name = "May cap",
            TargetAmount = 500,
            Deadline = "2026-05-31"
        };

        var status = SpendingGoalService.EvaluateBudgetCapGoal(goal, 501, new DateTime(2026, 5, 1));

        Assert.Equal(GoalStatus.Missed, status);
    }
}
