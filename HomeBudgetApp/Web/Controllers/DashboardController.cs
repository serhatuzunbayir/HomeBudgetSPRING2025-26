using Microsoft.AspNetCore.Mvc;
using SharedData;
using Web.Models.ViewModels;

namespace Web.Controllers;

public sealed class DashboardController : AuthenticatedController
{
    public DashboardController(IDatabaseRepository database)
        : base(database)
    {
    }

    [HttpGet]
    // Builds the complete dashboard view for the signed-in user.
    public IActionResult Index(int? categoryId, ExpenseSortOption sortOption = ExpenseSortOption.NewestFirst)
    {
        if (!TryGetCurrentUser(out var user))
        {
            return RedirectToLogin();
        }

        Database.EvaluateGoalsForUser(user.UserId);

        var categories = Database.GetAllCategoriesByUser(user.UserId)
            .OrderBy(category => category.Name)
            .ToList();
        var expenses = Database.GetAllExpensesByUser(user.UserId);
        var expensesByCategory = expenses
            .GroupBy(expense => expense.CategoryId)
            .ToDictionary(group => group.Key, group => group.ToList());
        var categoryCards = categories
            .Select(category =>
            {
                expensesByCategory.TryGetValue(category.CategoryId, out var categoryExpenses);
                return new DashboardCategoryCardViewModel(category, categoryExpenses ?? new List<Expense>());
            })
            .ToList();

        var selectedCategory = categories.FirstOrDefault(category => category.CategoryId == categoryId);
        var queryOptions = new ExpenseQueryOptions
        {
            CategoryId = selectedCategory?.CategoryId,
            SortOption = sortOption
        };
        var filteredExpenses = ExpenseInsightsService.FilterAndSortExpenses(expenses, queryOptions)
            .Select(expense => new ExpenseRowViewModel(expense))
            .ToList();
        var summaries = ExpenseInsightsService.BuildCategorySummaries(categories, expenses)
            .Select(summary => new CategoryExpenseSummaryViewModel(summary))
            .ToList();
        var activeAlerts = BudgetAlertService.GetActiveAlerts(categories);
        var goals = Database.GetGoalsByUser(user.UserId)
            .Select(goal => new GoalCardViewModel(goal))
            .ToList();

        var model = new DashboardViewModel
        {
            UserName = user.Name,
            CategoryCount = categoryCards.Count,
            TotalBudget = categoryCards.Sum(category => category.Budget),
            TotalSpent = categoryCards.Sum(category => category.Spent),
            RemainingBudget = categoryCards.Sum(category => category.Remaining),
            ThisMonthExpenses = expenses.Where(ExpenseInsightsService.IsInCurrentMonth).Sum(expense => expense.Amount),
            MonthlyAverage = ExpenseInsightsService.CalculateMonthlyAverage(expenses),
            BudgetAlertsText = BuildBudgetAlertText(activeAlerts),
            FamilyOverviewText = BuildFamilyOverviewText(user.UserId),
            SelectedCategoryId = selectedCategory?.CategoryId,
            SelectedSortOption = sortOption,
            Categories = categoryCards,
            CategorySummaries = summaries,
            FilteredExpenses = filteredExpenses,
            Goals = goals,
            GoalEvents = Database.GetRecentGoalEvents(user.UserId).Select(goalEvent => new GoalEventViewModel(goalEvent)).ToList(),
            CategoryFilterOptions = BuildCategoryFilterOptions(categories),
            SortOptions = BuildSortOptions(),
            ExpenseSummaryCaption = BuildExpenseSummaryCaption(summaries),
            ExpenseExplorerCaption = $"{filteredExpenses.Count} expense{(filteredExpenses.Count == 1 ? "" : "s")} · {(selectedCategory?.Name ?? "All categories")} · sorted by {GetSortLabel(sortOption).ToLowerInvariant()}",
            GoalsCaption = BuildGoalsCaption(goals)
        };

        return View(model);
    }

    private string BuildFamilyOverviewText(int userId)
    {
        var family = Database.GetFamilyByUser(userId);
        if (family is null)
        {
            return "No family workspace connected yet.";
        }

        var memberCount = Database.GetFamilyUsers(family.FamilyId).Count;
        return $"{family.Name} · {memberCount} member{(memberCount == 1 ? "" : "s")}";
    }

    private static IReadOnlyList<ExpenseFilterOptionViewModel> BuildCategoryFilterOptions(IEnumerable<Category> categories)
    {
        return new[] { new ExpenseFilterOptionViewModel(null, "All categories") }
            .Concat(categories.Select(category => new ExpenseFilterOptionViewModel(category.CategoryId, category.Name)))
            .ToList();
    }

    private static IReadOnlyList<ExpenseSortOptionViewModel> BuildSortOptions()
    {
        return new[]
        {
            new ExpenseSortOptionViewModel(ExpenseSortOption.NewestFirst, "Newest first"),
            new ExpenseSortOptionViewModel(ExpenseSortOption.OldestFirst, "Oldest first"),
            new ExpenseSortOptionViewModel(ExpenseSortOption.HighestAmount, "Highest amount"),
            new ExpenseSortOptionViewModel(ExpenseSortOption.LowestAmount, "Lowest amount")
        };
    }

    private static string GetSortLabel(ExpenseSortOption sortOption)
    {
        return BuildSortOptions().First(option => option.SortOption == sortOption).Label;
    }

    private static string BuildBudgetAlertText(IReadOnlyList<BudgetAlertNotification> alerts)
    {
        var lines = alerts
            .Select(alert => alert.IsOverBudget
                ? $"{alert.CategoryName} is over budget by {Math.Abs(alert.RemainingBudget):C}."
                : $"{alert.CategoryName} dropped below {alert.WarningThreshold:C} and has {alert.RemainingBudget:C} left.")
            .ToList();

        return lines.Count == 0
            ? "All categories are within a healthy range."
            : string.Join(Environment.NewLine, lines);
    }

    private static string BuildExpenseSummaryCaption(IReadOnlyCollection<CategoryExpenseSummaryViewModel> summaries)
    {
        if (summaries.Count == 0)
        {
            return "Add expenses to calculate totals and monthly averages.";
        }

        if (summaries.All(summary => summary.TotalSpent <= 0))
        {
            return "No spending recorded yet.";
        }

        var topCategory = summaries
            .OrderByDescending(summary => summary.TotalSpent)
            .ThenBy(summary => summary.CategoryName)
            .First();

        return $"Top spending category: {topCategory.CategoryName} at {topCategory.TotalSpentDisplay}.";
    }

    private static string BuildGoalsCaption(IReadOnlyCollection<GoalCardViewModel> goals)
    {
        if (goals.Count == 0)
        {
            return "Create a spending cap with a deadline to track progress automatically.";
        }

        var activeGoals = goals.Count(goal => goal.Status == GoalStatus.Active);
        return $"{activeGoals} active of {goals.Count} goal{(goals.Count == 1 ? "" : "s")}.";
    }
}
