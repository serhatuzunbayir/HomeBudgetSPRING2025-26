using System.Collections.ObjectModel;
using System.Globalization;
using SharedData;

namespace MAUIdesktop;

public partial class MainPage : ContentPage
{
    private readonly DatabaseRepository _db;
    private readonly ObservableCollection<CategoryCardViewModel> _categories = new();
    private readonly ObservableCollection<ExpenseRowViewModel> _recentExpenses = new();

    public MainPage()
    {
        InitializeComponent();

        _db = AppDatabase.Instance;
        CategoryCollection.ItemsSource = _categories;
        RecentExpensesCollection.ItemsSource = _recentExpenses;
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

        var allExpenses = _db.GetAllExpensesByUser(user!.UserId);
        var expenses = allExpenses
            .GroupBy(expense => expense.CategoryId)
            .ToDictionary(group => group.Key, group => group.ToList());

        foreach (var category in _db.GetAllCategoriesByUser(user.UserId))
        {
            expenses.TryGetValue(category.CategoryId, out var categoryExpenses);
            _categories.Add(new CategoryCardViewModel(category, categoryExpenses ?? new List<Expense>()));
        }

        var totalBudget = _categories.Sum(category => category.Budget);
        var totalSpent = _categories.Sum(category => category.Spent);
        var remainingBudget = _categories.Sum(category => category.Remaining);
        var monthlyAverage = SpendingSummaryCalculator.CalculateMonthlyAverage(allExpenses);
        var thisMonthExpenses = allExpenses
            .Where(SpendingSummaryCalculator.IsInCurrentMonth)
            .Sum(expense => expense.Amount);

        CategoryCountLabel.Text = _categories.Count.ToString();
        TotalBudgetLabel.Text = totalBudget.ToString("C");
        TotalSpentLabel.Text = totalSpent.ToString("C");
        RemainingBudgetLabel.Text = remainingBudget.ToString("C");
        ThisMonthExpensesLabel.Text = thisMonthExpenses.ToString("C");
        MonthlyAverageLabel.Text = monthlyAverage.ToString("C");
        BudgetAlertsLabel.Text = BuildBudgetAlertText(_categories);
        FamilyOverviewLabel.Text = BuildFamilyOverviewText(user.UserId);

        _recentExpenses.Clear();
        foreach (var expense in allExpenses
                     .OrderByDescending(expense => expense.Date)
                     .Take(6)
                     .Select(expense => new ExpenseRowViewModel(expense)))
        {
            _recentExpenses.Add(expense);
        }
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

        try
        {
            _db.AddCategory(user.UserId, name, budget);
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

        _db.AddExpense(user.UserId, category.CategoryId, description, amount, DateTime.Now.ToString("yyyy-MM-dd"));
        LoadDashboard();
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

    private static string BuildBudgetAlertText(IEnumerable<CategoryCardViewModel> categories)
    {
        var alerts = categories
            .Where(category => category.Budget > 0 && category.Remaining <= category.Budget * 0.2)
            .Select(category => category.Remaining < 0
                ? $"{category.Name} is over budget"
                : $"{category.Name} is close to its limit")
            .ToList();

        return alerts.Count == 0
            ? "All categories are within a healthy range."
            : string.Join(Environment.NewLine, alerts);
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
        CategoryId = category.CategoryId;
        Name = category.Name;
        RemainingBudget = category.RemainingBudget;
        MonthlyAverage = SpendingSummaryCalculator.CalculateMonthlyAverage(expenses);
        Expenses = new ObservableCollection<ExpenseRowViewModel>(
            expenses.OrderByDescending(expense => expense.Date).Select(expense => new ExpenseRowViewModel(expense)));
    }

    public int CategoryId { get; }
    public string Name { get; }
    public double RemainingBudget { get; }
    public double Budget => RemainingBudget + Spent;
    public double Spent => Expenses.Sum(expense => expense.Amount);
    public double MonthlyAverage { get; }
    public double Remaining => RemainingBudget;
    public double SpentProgress => Budget <= 0 ? 0 : Math.Min(1, Spent / Budget);
    public string BudgetDisplay => $"Budget: {Budget:C}";
    public string SpentDisplay => $"Spent: {Spent:C}";
    public string MonthlyAverageDisplay => $"Monthly avg: {MonthlyAverage:C}";
    public string RemainingDisplay => $"Remaining: {Remaining:C}";
    public string StatusDisplay => Remaining < 0
        ? "Over budget"
        : Budget > 0 && Remaining <= Budget * 0.2
            ? "Close to limit"
            : "On track";
    public Color StatusColor => Remaining < 0
        ? Color.FromArgb("#B91C1C")
        : Budget > 0 && Remaining <= Budget * 0.2
            ? Color.FromArgb("#B45309")
            : Color.FromArgb("#15803D");
    public ObservableCollection<ExpenseRowViewModel> Expenses { get; }
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

public static class SpendingSummaryCalculator
{
    public static double CalculateMonthlyAverage(IEnumerable<Expense> expenses)
    {
        return expenses
            .GroupBy(GetExpenseMonth)
            .Select(month => month.Sum(expense => expense.Amount))
            .DefaultIfEmpty(0)
            .Average();
    }

    private static string GetExpenseMonth(Expense expense)
    {
        return DateTime.TryParse(expense.Date, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            ? date.ToString("yyyy-MM", CultureInfo.InvariantCulture)
            : expense.Date;
    }

    public static bool IsInCurrentMonth(Expense expense)
    {
        return DateTime.TryParse(expense.Date, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) &&
               date.Year == DateTime.Now.Year &&
               date.Month == DateTime.Now.Month;
    }
}
