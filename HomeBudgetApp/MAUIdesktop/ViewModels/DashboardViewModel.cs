using System.Collections.ObjectModel;
using System.Globalization;
using SharedData;

namespace MAUIdesktop;

public sealed class DashboardViewModel : ObservableObject
{
    private readonly IDatabaseRepository _database;
    private readonly IUserDialogService _dialogs;
    private readonly IAppNavigationService _navigation;
    private List<Category> _allCategories = new();
    private List<Expense> _allExpenses = new();
    private ExpenseFilterOptionViewModel? _selectedExpenseFilter;
    private ExpenseSortOptionViewModel? _selectedExpenseSort;
    private string _welcomeText = "";
    private string _categoryCountText = "0";
    private string _totalBudgetText = 0.ToString("C");
    private string _totalSpentText = 0.ToString("C");
    private string _remainingBudgetText = 0.ToString("C");
    private string _thisMonthExpensesText = 0.ToString("C");
    private string _monthlyAverageText = 0.ToString("C");
    private string _budgetAlertsText = "";
    private Color _budgetAlertsTextColor = Colors.ForestGreen;
    private string _expenseSummaryCaption = "";
    private string _expenseExplorerCaption = "";
    private string _familyOverviewText = "";
    private string _goalsCaption = "";
    private string _goalEventsCaption = "";
    private bool _noCategoriesVisible;
    private string _goalName = "";
    private string _goalTargetAmount = "";
    private DateTime _goalDeadlineDate = DateTime.Today.AddMonths(1);

    public DashboardViewModel(IDatabaseRepository database, IUserDialogService dialogs, IAppNavigationService navigation)
    {
        _database = database;
        _dialogs = dialogs;
        _navigation = navigation;

        ExpenseSortOptions.Add(new ExpenseSortOptionViewModel(ExpenseSortOption.NewestFirst, "Newest first"));
        ExpenseSortOptions.Add(new ExpenseSortOptionViewModel(ExpenseSortOption.OldestFirst, "Oldest first"));
        ExpenseSortOptions.Add(new ExpenseSortOptionViewModel(ExpenseSortOption.HighestAmount, "Highest amount"));
        ExpenseSortOptions.Add(new ExpenseSortOptionViewModel(ExpenseSortOption.LowestAmount, "Lowest amount"));
        SelectedExpenseSort = ExpenseSortOptions[0];
    }

    public ObservableCollection<CategoryCardViewModel> CategoryCards { get; } = new();
    public ObservableCollection<CategoryExpenseSummaryViewModel> CategorySummaryCards { get; } = new();
    public ObservableCollection<ExpenseRowViewModel> RecentExpenseRows { get; } = new();
    public ObservableCollection<GoalCardViewModel> GoalCards { get; } = new();
    public ObservableCollection<GoalEventViewModel> GoalEventRows { get; } = new();
    public ObservableCollection<ExpenseFilterOptionViewModel> ExpenseFilterOptions { get; } = new();
    public ObservableCollection<ExpenseSortOptionViewModel> ExpenseSortOptions { get; } = new();

    public string WelcomeText { get => _welcomeText; private set => SetProperty(ref _welcomeText, value); }
    public string CategoryCountText { get => _categoryCountText; private set => SetProperty(ref _categoryCountText, value); }
    public string TotalBudgetText { get => _totalBudgetText; private set => SetProperty(ref _totalBudgetText, value); }
    public string TotalSpentText { get => _totalSpentText; private set => SetProperty(ref _totalSpentText, value); }
    public string RemainingBudgetText { get => _remainingBudgetText; private set => SetProperty(ref _remainingBudgetText, value); }
    public string ThisMonthExpensesText { get => _thisMonthExpensesText; private set => SetProperty(ref _thisMonthExpensesText, value); }
    public string MonthlyAverageText { get => _monthlyAverageText; private set => SetProperty(ref _monthlyAverageText, value); }
    public string BudgetAlertsText { get => _budgetAlertsText; private set => SetProperty(ref _budgetAlertsText, value); }
    public Color BudgetAlertsTextColor { get => _budgetAlertsTextColor; private set => SetProperty(ref _budgetAlertsTextColor, value); }
    public string ExpenseSummaryCaption { get => _expenseSummaryCaption; private set => SetProperty(ref _expenseSummaryCaption, value); }
    public string ExpenseExplorerCaption { get => _expenseExplorerCaption; private set => SetProperty(ref _expenseExplorerCaption, value); }
    public string FamilyOverviewText { get => _familyOverviewText; private set => SetProperty(ref _familyOverviewText, value); }
    public string GoalsCaption { get => _goalsCaption; private set => SetProperty(ref _goalsCaption, value); }
    public string GoalEventsCaption { get => _goalEventsCaption; private set => SetProperty(ref _goalEventsCaption, value); }
    public bool NoCategoriesVisible { get => _noCategoriesVisible; private set => SetProperty(ref _noCategoriesVisible, value); }
    public string GoalName { get => _goalName; set => SetProperty(ref _goalName, value); }
    public string GoalTargetAmount { get => _goalTargetAmount; set => SetProperty(ref _goalTargetAmount, value); }
    public DateTime GoalDeadlineDate { get => _goalDeadlineDate; set => SetProperty(ref _goalDeadlineDate, value); }
    public DateTime MinimumGoalDeadlineDate => DateTime.Today;

    public ExpenseFilterOptionViewModel? SelectedExpenseFilter
    {
        get => _selectedExpenseFilter;
        set => SetProperty(ref _selectedExpenseFilter, value);
    }

    public ExpenseSortOptionViewModel? SelectedExpenseSort
    {
        get => _selectedExpenseSort;
        set => SetProperty(ref _selectedExpenseSort, value);
    }

    public bool IsSignedIn => AppSession.CurrentUser is not null;

    // Reloads all personal dashboard data after sign-in or a budget change.
    public void LoadDashboard()
    {
        var user = AppSession.CurrentUser;
        if (user is null)
        {
            return;
        }

        AppSession.RefreshUser(_database);
        user = AppSession.CurrentUser;
        if (user is null)
        {
            return;
        }

        // Keep the user's selected filters after the dashboard reloads.
        var selectedCategoryId = SelectedExpenseFilter?.CategoryId;
        var selectedSortOption = SelectedExpenseSort?.SortOption ?? ExpenseSortOption.NewestFirst;

        CategoryCards.Clear();
        CategorySummaryCards.Clear();
        GoalCards.Clear();
        GoalEventRows.Clear();

        _database.EvaluateGoalsForUser(user.UserId);
        _allCategories = _database.GetAllCategoriesByUser(user.UserId)
            .OrderBy(category => category.Name)
            .ToList();
        _allExpenses = _database.GetAllExpensesByUser(user.UserId);
        // Group expenses once so each category card can find its rows quickly.
        var expenses = _allExpenses
            .GroupBy(expense => expense.CategoryId)
            .ToDictionary(group => group.Key, group => group.ToList());

        // Rebuild the visible category cards from the latest database values.
        foreach (var category in _allCategories)
        {
            expenses.TryGetValue(category.CategoryId, out var categoryExpenses);
            CategoryCards.Add(new CategoryCardViewModel(category, categoryExpenses ?? new List<Expense>()));
        }

        // Active alerts control both the alert text and its warning color.
        var activeAlerts = BudgetAlertService.GetActiveAlerts(CategoryCards.Select(category => category.Category));
        WelcomeText = $"Welcome, {user.Name}";
        CategoryCountText = CategoryCards.Count.ToString();
        TotalBudgetText = CategoryCards.Sum(category => category.Budget).ToString("C");
        TotalSpentText = CategoryCards.Sum(category => category.Spent).ToString("C");
        RemainingBudgetText = CategoryCards.Sum(category => category.Remaining).ToString("C");
        ThisMonthExpensesText = _allExpenses.Where(ExpenseInsightsService.IsInCurrentMonth).Sum(expense => expense.Amount).ToString("C");
        MonthlyAverageText = ExpenseInsightsService.CalculateMonthlyAverage(_allExpenses).ToString("C");
        BudgetAlertsText = BuildBudgetAlertText(activeAlerts);
        BudgetAlertsTextColor = activeAlerts.Count == 0 ? Colors.ForestGreen : Colors.DarkOrange;
        FamilyOverviewText = BuildFamilyOverviewText(user.UserId);
        NoCategoriesVisible = CategoryCards.Count == 0;

        foreach (var goal in _database.GetGoalsByUser(user.UserId).Select(goal => new GoalCardViewModel(goal)))
        {
            GoalCards.Add(goal);
        }

        foreach (var goalEvent in _database.GetRecentGoalEvents(user.UserId).Select(goalEvent => new GoalEventViewModel(goalEvent)))
        {
            GoalEventRows.Add(goalEvent);
        }

        GoalsCaption = BuildGoalsCaption(GoalCards);
        GoalEventsCaption = GoalEventRows.Count == 0 ? "No goal events yet." : "Recent goal milestones.";
        RefreshExpenseFilterOptions(selectedCategoryId);
        RestoreExpenseSortSelection(selectedSortOption);
        RefreshExpenseInsights();
    }

    public async Task AddCategoryAsync()
    {
        var user = AppSession.CurrentUser;
        if (user is null)
        {
            return;
        }

        var name = (await _dialogs.PromptAsync("New category", "Category name", "Add", "Cancel", "Groceries") ?? "").Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        var budgetText = await _dialogs.PromptAsync("New category", "Budget", "Add", "Cancel", "1000", Keyboard.Numeric);
        if (!TryReadMoney(budgetText, out var budget) || budget < 0)
        {
            await _dialogs.AlertAsync("Invalid budget", "Please enter a valid positive number.");
            return;
        }

        // Suggest 20 percent of the budget as a starter alert threshold.
        var thresholdText = await _dialogs.PromptAsync(
            "New category",
            "Alert threshold for remaining budget (optional)",
            "Add",
            "Skip",
            budget > 0 ? (budget * 0.2).ToString("0.##", CultureInfo.InvariantCulture) : "",
            Keyboard.Numeric);

        if (!TryReadOptionalMoney(thresholdText, out var warningThreshold) || warningThreshold < 0)
        {
            await _dialogs.AlertAsync("Invalid threshold", "Enter a valid positive number or leave it blank.");
            return;
        }

        try
        {
            _database.AddCategory(user.UserId, name, budget, warningThreshold);
            LoadDashboard();
        }
        catch (Exception ex)
        {
            await _dialogs.AlertAsync("Category not added", ex.Message);
        }
    }

    // Adds an expense and warns the user if it crosses a category alert threshold.
    public async Task AddExpenseAsync(CategoryCardViewModel category)
    {
        var user = AppSession.CurrentUser;
        if (user is null)
        {
            return;
        }

        var description = (await _dialogs.PromptAsync("New expense", "Description", "Add", "Cancel", "Weekly groceries") ?? "").Trim();
        if (string.IsNullOrWhiteSpace(description))
        {
            return;
        }

        var amountText = await _dialogs.PromptAsync("New expense", "Enter expense amount", "Add", "Cancel", "150", Keyboard.Numeric);
        if (!TryReadMoney(amountText, out var amount) || amount <= 0)
        {
            await _dialogs.AlertAsync("Invalid amount", "Please enter a valid amount greater than zero.");
            return;
        }

        // Compare the category before and after the expense to detect threshold crossings.
        var previousCategory = _database.GetCategory(category.CategoryId, user.UserId);
        _database.AddExpense(user.UserId, category.CategoryId, description, amount, DateTime.Now.ToString("yyyy-MM-dd"));
        var updatedCategory = _database.GetCategory(category.CategoryId, user.UserId);
        LoadDashboard();

        if (previousCategory is not null && updatedCategory is not null)
        {
            await BudgetAlertService.NotifyIfThresholdCrossedAsync(
                previousCategory,
                updatedCategory,
                notification => _dialogs.AlertAsync(notification.Title, notification.Message));
        }
    }

    public async Task DeleteCategoryAsync(CategoryCardViewModel category)
    {
        var user = AppSession.CurrentUser;
        if (user is null)
        {
            return;
        }

        var confirmed = await _dialogs.ConfirmAsync(
            "Delete category",
            $"Delete {category.Name} and all expenses inside it?",
            "Delete",
            "Cancel");

        if (!confirmed)
        {
            return;
        }

        _database.DeleteCategory(category.CategoryId, user.UserId);
        LoadDashboard();
    }

    public async Task SetAlertThresholdAsync(CategoryCardViewModel category)
    {
        var user = AppSession.CurrentUser;
        if (user is null)
        {
            return;
        }

        var thresholdText = await _dialogs.PromptAsync(
            "Budget alert",
            $"Warning threshold for {category.Name} (leave blank to disable)",
            "Save",
            "Cancel",
            category.WarningThreshold?.ToString("0.##", CultureInfo.InvariantCulture) ?? "",
            Keyboard.Numeric);

        if (thresholdText is null)
        {
            return;
        }

        if (!TryReadOptionalMoney(thresholdText, out var warningThreshold) || warningThreshold < 0)
        {
            await _dialogs.AlertAsync("Invalid threshold", "Enter a valid positive number or leave it blank.");
            return;
        }

        _database.UpdateCategory(category.CategoryId, user.UserId, category.Name, category.RemainingBudget, warningThreshold);
        LoadDashboard();
    }

    public async Task DeleteExpenseAsync(ExpenseRowViewModel expense)
    {
        var user = AppSession.CurrentUser;
        if (user is null)
        {
            return;
        }

        var confirmed = await _dialogs.ConfirmAsync("Delete expense", $"Delete {expense.Description}?", "Delete", "Cancel");
        if (!confirmed)
        {
            return;
        }

        _database.DeleteExpense(expense.ExpenseId, user.UserId);
        LoadDashboard();
    }

    public async Task AddGoalAsync()
    {
        var user = AppSession.CurrentUser;
        if (user is null)
        {
            return;
        }

        var name = (GoalName ?? "").Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            await _dialogs.AlertAsync("Goal name required", "Enter a name for this spending goal.");
            return;
        }

        if (!TryReadMoney(GoalTargetAmount, out var targetAmount) || targetAmount <= 0)
        {
            await _dialogs.AlertAsync("Invalid amount", "Please enter a spending cap greater than zero.");
            return;
        }

        _database.AddGoal(user.UserId, name, targetAmount, GoalDeadlineDate);
        GoalName = "";
        GoalTargetAmount = "";
        GoalDeadlineDate = DateTime.Today.AddMonths(1);
        LoadDashboard();
    }

    public async Task DeleteGoalAsync(GoalCardViewModel goal)
    {
        var user = AppSession.CurrentUser;
        if (user is null)
        {
            return;
        }

        var confirmed = await _dialogs.ConfirmAsync("Delete goal", $"Delete {goal.Name}?", "Delete", "Cancel");
        if (!confirmed)
        {
            return;
        }

        _database.DeleteGoal(goal.GoalId, user.UserId);
        LoadDashboard();
    }

    public Task OpenProfileAsync() => _navigation.GoToAsync(nameof(ProfilePage));

    public Task OpenFamilyAsync() => _navigation.GoToAsync("//FamilyPage");

    public async Task LogoutAsync()
    {
        AppSession.SignOut();
        await _navigation.GoToAsync("//LoginPage");
    }

    // Rebuilds the filtered expense list and category summary cards.
    public void RefreshExpenseInsights()
    {
        var selectedSort = SelectedExpenseSort;
        if (selectedSort is null)
        {
            return;
        }

        var queryOptions = new ExpenseQueryOptions
        {
            CategoryId = SelectedExpenseFilter?.CategoryId,
            SortOption = selectedSort.SortOption
        };

        ExpenseInsightsService.NotifyInsightsUpdated(
            _allCategories,
            _allExpenses,
            queryOptions,
            SelectedExpenseFilter?.Label ?? "All categories",
            selectedSort.Label,
            ApplyExpenseInsightsSnapshot);
    }

    private void RefreshExpenseFilterOptions(int? selectedCategoryId)
    {
        ExpenseFilterOptions.Clear();
        ExpenseFilterOptions.Add(new ExpenseFilterOptionViewModel(null, "All categories"));
        foreach (var category in _allCategories)
        {
            ExpenseFilterOptions.Add(new ExpenseFilterOptionViewModel(category.CategoryId, category.Name));
        }

        SelectedExpenseFilter = ExpenseFilterOptions
            .FirstOrDefault(option => option.CategoryId == selectedCategoryId)
            ?? ExpenseFilterOptions[0];
    }

    private void RestoreExpenseSortSelection(ExpenseSortOption sortOption)
    {
        SelectedExpenseSort = ExpenseSortOptions
            .FirstOrDefault(option => option.SortOption == sortOption)
            ?? ExpenseSortOptions[0];
    }

    private void ApplyExpenseInsightsSnapshot(ExpenseInsightsSnapshot snapshot)
    {
        CategorySummaryCards.Clear();
        foreach (var summary in snapshot.CategorySummaries.Select(summary => new CategoryExpenseSummaryViewModel(summary)))
        {
            CategorySummaryCards.Add(summary);
        }

        var displayedExpenses = snapshot.FilteredExpenses.ToList();

        RecentExpenseRows.Clear();
        foreach (var expense in displayedExpenses.Select(expense => new ExpenseRowViewModel(expense)))
        {
            RecentExpenseRows.Add(expense);
        }

        ExpenseSummaryCaption = BuildExpenseSummaryCaption(CategorySummaryCards);
        ExpenseExplorerCaption = $"{displayedExpenses.Count} expense{(displayedExpenses.Count == 1 ? "" : "s")} · {snapshot.ScopeLabel} · sorted by {snapshot.SortLabel.ToLowerInvariant()}";
    }

    private string BuildFamilyOverviewText(int userId)
    {
        var family = _database.GetFamilyByUser(userId);
        if (family is null)
        {
            return "No family workspace connected yet.";
        }

        var memberCount = _database.GetFamilyUsers(family.FamilyId).Count;
        return $"{family.Name} · {memberCount} member{(memberCount == 1 ? "" : "s")}";
    }

    private static bool TryReadMoney(string? value, out double amount)
    {
        value = (value ?? "").Trim().Replace(',', '.');
        return double.TryParse(
            value,
            NumberStyles.Number,
            CultureInfo.InvariantCulture,
            out amount);
    }

    private static bool TryReadOptionalMoney(string? value, out double? amount)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            amount = null;
            return true;
        }

        if (TryReadMoney(value, out var parsedAmount))
        {
            amount = parsedAmount;
            return true;
        }

        amount = null;
        return false;
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
