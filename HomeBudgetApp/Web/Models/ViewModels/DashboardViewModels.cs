using SharedData;

namespace Web.Models.ViewModels;

public sealed class DashboardViewModel
{
    public string UserName { get; init; } = "";
    public int CategoryCount { get; init; }
    public double TotalBudget { get; init; }
    public double TotalSpent { get; init; }
    public double RemainingBudget { get; init; }
    public double ThisMonthExpenses { get; init; }
    public double MonthlyAverage { get; init; }
    public string BudgetAlertsText { get; init; } = "";
    public string FamilyOverviewText { get; init; } = "";
    public int? SelectedCategoryId { get; init; }
    public ExpenseSortOption SelectedSortOption { get; init; }
    public IReadOnlyList<DashboardCategoryCardViewModel> Categories { get; init; } = Array.Empty<DashboardCategoryCardViewModel>();
    public IReadOnlyList<CategoryExpenseSummaryViewModel> CategorySummaries { get; init; } = Array.Empty<CategoryExpenseSummaryViewModel>();
    public IReadOnlyList<ExpenseRowViewModel> FilteredExpenses { get; init; } = Array.Empty<ExpenseRowViewModel>();
    public IReadOnlyList<GoalCardViewModel> Goals { get; init; } = Array.Empty<GoalCardViewModel>();
    public IReadOnlyList<GoalEventViewModel> GoalEvents { get; init; } = Array.Empty<GoalEventViewModel>();
    public IReadOnlyList<ExpenseFilterOptionViewModel> CategoryFilterOptions { get; init; } = Array.Empty<ExpenseFilterOptionViewModel>();
    public IReadOnlyList<ExpenseSortOptionViewModel> SortOptions { get; init; } = Array.Empty<ExpenseSortOptionViewModel>();
    public string ExpenseSummaryCaption { get; init; } = "";
    public string ExpenseExplorerCaption { get; init; } = "";
    public string GoalsCaption { get; init; } = "";
}

public sealed class DashboardCategoryCardViewModel
{
    public DashboardCategoryCardViewModel(Category category, IReadOnlyCollection<Expense> expenses)
    {
        CategoryId = category.CategoryId;
        Name = category.Name;
        RemainingBudget = category.RemainingBudget;
        WarningThreshold = category.WarningThreshold;
        MonthlyAverage = ExpenseInsightsService.CalculateMonthlyAverage(expenses);
        Expenses = expenses
            .OrderByDescending(expense => expense.Date)
            .Select(expense => new ExpenseRowViewModel(expense))
            .ToList();
    }

    public int CategoryId { get; }
    public string Name { get; }
    public double RemainingBudget { get; }
    public double? WarningThreshold { get; }
    public double Budget => RemainingBudget + Spent;
    public double Spent => Expenses.Sum(expense => expense.Amount);
    public double MonthlyAverage { get; }
    public double Remaining => RemainingBudget;
    public string BudgetDisplay => $"Budget: {Budget:C}";
    public string SpentDisplay => $"Spent: {Spent:C}";
    public string MonthlyAverageDisplay => $"Monthly avg: {MonthlyAverage:C}";
    public string RemainingDisplay => $"Remaining: {Remaining:C}";
    public string AlertThresholdDisplay => WarningThreshold.HasValue
        ? $"Alert below {WarningThreshold.Value:C}"
        : "Alert disabled";
    public string StatusDisplay => Remaining < 0
        ? "Over budget"
        : WarningThreshold.HasValue && Remaining < WarningThreshold.Value
            ? "Below alert threshold"
            : "On track";
    public string StatusClass => Remaining < 0
        ? "text-danger"
        : WarningThreshold.HasValue && Remaining < WarningThreshold.Value
            ? "text-warning"
            : "text-success";
    public IReadOnlyList<ExpenseRowViewModel> Expenses { get; }
}

public sealed class CategoryExpenseSummaryViewModel
{
    public CategoryExpenseSummaryViewModel(CategoryExpenseSummary summary)
    {
        CategoryId = summary.CategoryId;
        CategoryName = summary.CategoryName;
        TotalSpent = summary.TotalExpenses;
        MonthlyAverage = summary.MonthlyAverage;
        ExpenseCount = summary.ExpenseCount;
    }

    public int CategoryId { get; }
    public string CategoryName { get; }
    public double TotalSpent { get; }
    public double MonthlyAverage { get; }
    public int ExpenseCount { get; }
    public string TotalSpentDisplay => TotalSpent.ToString("C");
    public string MonthlyAverageDisplay => $"Monthly avg {MonthlyAverage:C}";
    public string ExpenseCountDisplay => $"{ExpenseCount} expense{(ExpenseCount == 1 ? "" : "s")}";
}

public sealed class ExpenseRowViewModel
{
    public ExpenseRowViewModel(Expense expense)
    {
        ExpenseId = expense.ExpenseId;
        CategoryId = expense.CategoryId;
        Description = expense.Description;
        Amount = expense.Amount;
        Date = expense.Date;
    }

    public int ExpenseId { get; }
    public int CategoryId { get; }
    public string Description { get; }
    public double Amount { get; }
    public string AmountDisplay => Amount.ToString("C");
    public string Date { get; }
}

public sealed class GoalCardViewModel
{
    public GoalCardViewModel(Goal goal)
    {
        GoalId = goal.GoalId;
        Name = goal.Name;
        TargetAmount = goal.TargetAmount;
        CurrentSpent = goal.CurrentSpent;
        RemainingAmount = goal.RemainingAmount;
        ProgressRatio = goal.ProgressRatio;
        Deadline = goal.Deadline;
        Status = goal.Status;
    }

    public int GoalId { get; }
    public string Name { get; }
    public double TargetAmount { get; }
    public double CurrentSpent { get; }
    public double RemainingAmount { get; }
    public double ProgressRatio { get; }
    public int ProgressPercent => (int)Math.Round(ProgressRatio * 100);
    public string Deadline { get; }
    public GoalStatus Status { get; }
    public string DeadlineDisplay => $"Deadline: {Deadline}";
    public string ProgressDisplay => $"{CurrentSpent:C} spent of {TargetAmount:C} cap · {RemainingAmount:C} left";
    public string StatusDisplay => Status.ToString();
}

public sealed class GoalEventViewModel
{
    public GoalEventViewModel(GoalEvent goalEvent)
    {
        Message = goalEvent.Message;
        CreatedAt = goalEvent.CreatedAt;
    }

    public string Message { get; }
    public string CreatedAt { get; }
}

public sealed class ExpenseFilterOptionViewModel
{
    public ExpenseFilterOptionViewModel(int? categoryId, string label)
    {
        CategoryId = categoryId;
        Label = label;
    }

    public int? CategoryId { get; }
    public string Label { get; }
}

public sealed class ExpenseSortOptionViewModel
{
    public ExpenseSortOptionViewModel(ExpenseSortOption sortOption, string label)
    {
        SortOption = sortOption;
        Label = label;
    }

    public ExpenseSortOption SortOption { get; }
    public string Label { get; }
}
