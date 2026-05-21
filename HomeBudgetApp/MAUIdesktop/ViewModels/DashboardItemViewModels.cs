using System.Collections.ObjectModel;
using SharedData;

namespace MAUIdesktop;

public sealed class CategoryCardViewModel
{
    public CategoryCardViewModel(Category category, IReadOnlyCollection<Expense> expenses)
    {
        Category = category;
        CategoryId = category.CategoryId;
        Name = category.Name;
        RemainingBudget = category.RemainingBudget;
        WarningThreshold = category.WarningThreshold;
        MonthlyAverage = ExpenseInsightsService.CalculateMonthlyAverage(expenses);
        Expenses = new ObservableCollection<ExpenseRowViewModel>(
            expenses.OrderByDescending(expense => expense.Date).Select(expense => new ExpenseRowViewModel(expense)));
    }

    public Category Category { get; }
    public int CategoryId { get; }
    public string Name { get; }
    public double RemainingBudget { get; }
    public double? WarningThreshold { get; }
    public double Budget => RemainingBudget + Spent;
    public double Spent => Expenses.Sum(expense => expense.Amount);
    public double MonthlyAverage { get; }
    public double Remaining => RemainingBudget;
    public double SpentProgress => Budget <= 0 ? 0 : Math.Min(1, Spent / Budget);
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
    public Color StatusColor => Remaining < 0
        ? Color.FromArgb("#B91C1C")
        : WarningThreshold.HasValue && Remaining < WarningThreshold.Value
            ? Color.FromArgb("#B45309")
            : Color.FromArgb("#15803D");
    public bool HasNoExpenses => Expenses.Count == 0;
    public ObservableCollection<ExpenseRowViewModel> Expenses { get; }
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
        Description = expense.Description;
        Amount = expense.Amount;
        Date = expense.Date;
    }

    public int ExpenseId { get; }
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
        CreatedAt = goal.CreatedAt;
        Deadline = goal.Deadline;
        Status = goal.Status;
    }

    public int GoalId { get; }
    public string Name { get; }
    public double TargetAmount { get; }
    public double CurrentSpent { get; }
    public double RemainingAmount { get; }
    public double ProgressRatio { get; }
    public string CreatedAt { get; }
    public string Deadline { get; }
    public GoalStatus Status { get; }
    public string DeadlineDisplay => $"Deadline: {Deadline}";
    public string ProgressDisplay => $"{CurrentSpent:C} spent of {TargetAmount:C} cap · {RemainingAmount:C} left";
    public string StatusDisplay => Status.ToString();
    public Color StatusColor => Status switch
    {
        GoalStatus.Reached => Color.FromArgb("#15803D"),
        GoalStatus.Missed => Color.FromArgb("#B91C1C"),
        _ => Color.FromArgb("#2563EB")
    };
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
