using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using SharedData;

namespace MAUIdesktop;

public partial class MainPage : ContentPage
{
    private readonly DatabaseRepository _db;
    private readonly ObservableCollection<CategoryCardViewModel> _categories = new();
    private readonly ObservableCollection<CategoryExpenseSummaryViewModel> _categorySummaries = new();
    private readonly ObservableCollection<ExpenseRowViewModel> _recentExpenses = new();
    private readonly List<ExpenseFilterOptionViewModel> _expenseFilterOptions = new();
    private readonly List<ExpenseSortOptionViewModel> _expenseSortOptions = new();
    private List<Category> _allCategories = new();
    private List<Expense> _allExpenses = new();

    public MainPage()
    {
        InitializeComponent();

        _db = AppDatabase.Instance;
        CategoryCollection.ItemsSource = _categories;
        ExpenseSummaryCollection.ItemsSource = _categorySummaries;
        RecentExpensesCollection.ItemsSource = _recentExpenses;

        _expenseSortOptions.AddRange(new[]
        {
            new ExpenseSortOptionViewModel(ExpenseSortOption.NewestFirst, "Newest first"),
            new ExpenseSortOptionViewModel(ExpenseSortOption.OldestFirst, "Oldest first"),
            new ExpenseSortOptionViewModel(ExpenseSortOption.HighestAmount, "Highest amount"),
            new ExpenseSortOptionViewModel(ExpenseSortOption.LowestAmount, "Lowest amount")
        });

        ExpenseSortPicker.ItemsSource = _expenseSortOptions;
        ExpenseSortPicker.SelectedItem = _expenseSortOptions[0];
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (AppSession.CurrentUser is null)
        {
            Shell.Current.GoToAsync("//LoginPage");
            return;
        }

        LoadDashboard();
    }

    private void LoadDashboard()
    {
        var user = AppSession.CurrentUser;
        if (user is null)
        {
            return;
        }

        AppSession.RefreshUser(_db);
        user = AppSession.CurrentUser;

        WelcomeLabel.Text = $"Welcome, {user?.Name}";
        _categories.Clear();
        _categorySummaries.Clear();

        var selectedCategoryId = (ExpenseCategoryFilterPicker.SelectedItem as ExpenseFilterOptionViewModel)?.CategoryId;
        var selectedSortOption = (ExpenseSortPicker.SelectedItem as ExpenseSortOptionViewModel)?.SortOption
                                 ?? ExpenseSortOption.NewestFirst;

        _allCategories = _db.GetAllCategoriesByUser(user!.UserId)
            .OrderBy(category => category.Name)
            .ToList();
        _allExpenses = _db.GetAllExpensesByUser(user.UserId);

        var expenses = _allExpenses
            .GroupBy(expense => expense.CategoryId)
            .ToDictionary(group => group.Key, group => group.ToList());

        foreach (var category in _allCategories)
        {
            expenses.TryGetValue(category.CategoryId, out var categoryExpenses);
            _categories.Add(new CategoryCardViewModel(category, categoryExpenses ?? new List<Expense>()));
        }

        var totalBudget = _categories.Sum(category => category.Budget);
        var totalSpent = _categories.Sum(category => category.Spent);
        var remainingBudget = _categories.Sum(category => category.Remaining);
        var monthlyAverage = ExpenseInsightsService.CalculateMonthlyAverage(_allExpenses);
        var activeAlerts = BudgetAlertService.GetActiveAlerts(_categories.Select(category => category.Category));
        var thisMonthExpenses = _allExpenses
            .Where(ExpenseInsightsService.IsInCurrentMonth)
            .Sum(expense => expense.Amount);

        CategoryCountLabel.Text = _categories.Count.ToString();
        TotalBudgetLabel.Text = totalBudget.ToString("C");
        TotalSpentLabel.Text = totalSpent.ToString("C");
        RemainingBudgetLabel.Text = remainingBudget.ToString("C");
        ThisMonthExpensesLabel.Text = thisMonthExpenses.ToString("C");
        MonthlyAverageLabel.Text = monthlyAverage.ToString("C");
        BudgetAlertsLabel.Text = BuildBudgetAlertText(activeAlerts);
        BudgetAlertsLabel.TextColor = activeAlerts.Count == 0 ? Colors.ForestGreen : Colors.DarkOrange;
        ExpenseSummaryCaptionLabel.Text = BuildExpenseSummaryCaption(_categorySummaries);
        FamilyOverviewLabel.Text = BuildFamilyOverviewText(user.UserId);

        RefreshExpenseFilterOptions(selectedCategoryId);
        RestoreExpenseSortSelection(selectedSortOption);
        RefreshExpenseInsights();
    }

    private async void OnAddCategoryClicked(object sender, EventArgs e)
    {
        var user = AppSession.CurrentUser;
        if (user is null)
        {
            return;
        }

        var name = await DisplayPromptAsync("New category", "Category name", "Add", "Cancel", "Groceries");
        name = (name ?? "").Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        var budgetText = await DisplayPromptAsync("New category", "Budget", "Add", "Cancel", "1000", keyboard: Keyboard.Numeric);
        if (!TryReadMoney(budgetText, out var budget) || budget < 0)
        {
            await DisplayAlert("Invalid budget", "Please enter a valid positive number.", "OK");
            return;
        }

        var thresholdText = await DisplayPromptAsync(
            "New category",
            "Alert threshold for remaining budget (optional)",
            "Add",
            "Skip",
            budget > 0 ? (budget * 0.2).ToString("0.##", CultureInfo.InvariantCulture) : "",
            keyboard: Keyboard.Numeric);

        if (!TryReadOptionalMoney(thresholdText, out var warningThreshold) || warningThreshold < 0)
        {
            await DisplayAlert("Invalid threshold", "Enter a valid positive number or leave it blank.", "OK");
            return;
        }

        try
        {
            _db.AddCategory(user.UserId, name, budget, warningThreshold);
            LoadDashboard();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Category not added", ex.Message, "OK");
        }
    }

    private async void OnAddExpenseClicked(object sender, EventArgs e)
    {
        var user = AppSession.CurrentUser;
        if (user is null || (sender as Button)?.CommandParameter is not CategoryCardViewModel category)
        {
            return;
        }

        var description = await DisplayPromptAsync("New expense", "Description", "Add", "Cancel", "Weekly groceries");
        description = (description ?? "").Trim();

        if (string.IsNullOrWhiteSpace(description))
        {
            return;
        }

        var amountText = await DisplayPromptAsync("New expense", "Enter expense amount", "Add", "Cancel", "150", keyboard: Keyboard.Numeric);
        if (!TryReadMoney(amountText, out var amount) || amount <= 0)
        {
            await DisplayAlert("Invalid amount", "Please enter a valid amount greater than zero.", "OK");
            return;
        }

        var previousCategory = _db.GetCategory(category.CategoryId, user.UserId);
        _db.AddExpense(user.UserId, category.CategoryId, description, amount, DateTime.Now.ToString("yyyy-MM-dd"));
        var updatedCategory = _db.GetCategory(category.CategoryId, user.UserId);
        LoadDashboard();

        if (previousCategory is not null && updatedCategory is not null)
        {
            // Pass the desktop popup method into the shared alert service.
            await BudgetAlertService.NotifyIfThresholdCrossedAsync(
                previousCategory,
                updatedCategory,
                ShowBudgetAlertNotificationAsync);
        }
    }

    private async void OnDeleteCategoryClicked(object sender, EventArgs e)
    {
        var user = AppSession.CurrentUser;
        if (user is null || (sender as Button)?.CommandParameter is not CategoryCardViewModel category)
        {
            return;
        }

        var confirmed = await DisplayAlert(
            "Delete category",
            $"Delete {category.Name} and all expenses inside it?",
            "Delete",
            "Cancel");

        if (!confirmed)
        {
            return;
        }

        _db.DeleteCategory(category.CategoryId, user.UserId);
        LoadDashboard();
    }

    private async void OnSetAlertThresholdClicked(object sender, EventArgs e)
    {
        var user = AppSession.CurrentUser;
        if (user is null || (sender as Button)?.CommandParameter is not CategoryCardViewModel category)
        {
            return;
        }

        var thresholdText = await DisplayPromptAsync(
            "Budget alert",
            $"Warning threshold for {category.Name} (leave blank to disable)",
            "Save",
            "Cancel",
            category.WarningThreshold?.ToString("0.##", CultureInfo.InvariantCulture) ?? "",
            keyboard: Keyboard.Numeric);

        if (thresholdText is null)
        {
            return;
        }

        if (!TryReadOptionalMoney(thresholdText, out var warningThreshold) || warningThreshold < 0)
        {
            await DisplayAlert("Invalid threshold", "Enter a valid positive number or leave it blank.", "OK");
            return;
        }

        _db.UpdateCategory(category.CategoryId, user.UserId, category.Name, category.RemainingBudget, warningThreshold);
        LoadDashboard();
    }

    private async void OnDeleteExpenseClicked(object sender, EventArgs e)
    {
        var user = AppSession.CurrentUser;
        if (user is null || (sender as Button)?.CommandParameter is not ExpenseRowViewModel expense)
        {
            return;
        }

        var confirmed = await DisplayAlert("Delete expense", $"Delete {expense.Description}?", "Delete", "Cancel");
        if (!confirmed)
        {
            return;
        }

        _db.DeleteExpense(expense.ExpenseId, user.UserId);
        LoadDashboard();
    }

    private async void OnProfileClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(ProfilePage));
    }

    private async void OnFamilyClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//FamilyPage");
    }

    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        AppSession.SignOut();
        await Shell.Current.GoToAsync("//LoginPage");
    }

    private static bool TryReadMoney(string? value, out double amount)
    {
        value = (value ?? "").Trim().Replace(',', '.');
        return double.TryParse(
            value,
            System.Globalization.NumberStyles.Number,
            System.Globalization.CultureInfo.InvariantCulture,
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

    private void RefreshExpenseFilterOptions(int? selectedCategoryId)
    {
        _expenseFilterOptions.Clear();
        _expenseFilterOptions.Add(new ExpenseFilterOptionViewModel(null, "All categories"));
        _expenseFilterOptions.AddRange(_allCategories.Select(category =>
            new ExpenseFilterOptionViewModel(category.CategoryId, category.Name)));

        ExpenseCategoryFilterPicker.ItemsSource = null;
        ExpenseCategoryFilterPicker.ItemsSource = _expenseFilterOptions;

        ExpenseCategoryFilterPicker.SelectedItem = _expenseFilterOptions
            .FirstOrDefault(option => option.CategoryId == selectedCategoryId)
            ?? _expenseFilterOptions[0];
    }

    private void RestoreExpenseSortSelection(ExpenseSortOption sortOption)
    {
        ExpenseSortPicker.SelectedItem = _expenseSortOptions
            .FirstOrDefault(option => option.SortOption == sortOption)
            ?? _expenseSortOptions[0];
    }

    private void RefreshExpenseInsights()
    {
        var selectedCategory = ExpenseCategoryFilterPicker.SelectedItem as ExpenseFilterOptionViewModel;
        var selectedSort = ExpenseSortPicker.SelectedItem as ExpenseSortOptionViewModel;
        if (selectedSort is null)
        {
            return;
        }

        var queryOptions = new ExpenseQueryOptions
        {
            CategoryId = selectedCategory?.CategoryId,
            SortOption = selectedSort.SortOption
        };

        // This delegate keeps the filter/sort logic separate from the UI widgets.
        ExpenseInsightsService.NotifyInsightsUpdated(
            _allCategories,
            _allExpenses,
            queryOptions,
            selectedCategory?.Label ?? "All categories",
            selectedSort.Label,
            ApplyExpenseInsightsSnapshot);
    }

    private void ApplyExpenseInsightsSnapshot(ExpenseInsightsSnapshot snapshot)
    {
        _categorySummaries.Clear();
        foreach (var summary in snapshot.CategorySummaries.Select(summary => new CategoryExpenseSummaryViewModel(summary)))
        {
            _categorySummaries.Add(summary);
        }

        var displayedExpenses = snapshot.FilteredExpenses.Take(6).ToList();

        _recentExpenses.Clear();
        foreach (var expense in displayedExpenses.Select(expense => new ExpenseRowViewModel(expense)))
        {
            _recentExpenses.Add(expense);
        }

        ExpenseSummaryCaptionLabel.Text = BuildExpenseSummaryCaption(_categorySummaries);
        ExpenseExplorerCaptionLabel.Text = BuildExpenseExplorerCaption(
            snapshot.ScopeLabel,
            snapshot.SortLabel,
            snapshot.FilteredExpenses.Count,
            displayedExpenses.Count);
    }

    private static string BuildExpenseExplorerCaption(
        string scopeLabel,
        string sortLabel,
        int matchingExpenseCount,
        int displayedExpenseCount)
    {
        return $"{displayedExpenseCount} of {matchingExpenseCount} expense{(matchingExpenseCount == 1 ? "" : "s")} · {scopeLabel} · sorted by {sortLabel.ToLowerInvariant()}";
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

    private void OnExpenseFilterChanged(object? sender, EventArgs e)
    {
        RefreshExpenseInsights();
    }

    private Task ShowBudgetAlertNotificationAsync(BudgetAlertNotification notification)
    {
        return DisplayAlert(notification.Title, notification.Message, "OK");
    }

    private string BuildFamilyOverviewText(int userId)
    {
        var family = _db.GetFamilyByUser(userId);
        if (family is null)
        {
            return "No family workspace connected yet.";
        }

        var memberCount = _db.GetFamilyUsers(family.FamilyId).Count;
        return $"{family.Name} · {memberCount} member{(memberCount == 1 ? "" : "s")}";
    }
}

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
