namespace MAUIdesktop;

public partial class MainPage : ContentPage
{
    private readonly DashboardViewModel _viewModel;

    public MainPage()
    {
        InitializeComponent();

        _viewModel = new DashboardViewModel(
            AppDatabase.Instance,
            new PageDialogService(this),
            new ShellNavigationService());
        BindingContext = _viewModel;
        GoalDeadlineDatePicker.MinimumDate = _viewModel.MinimumGoalDeadlineDate;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (! _viewModel.IsSignedIn)
        {
            Shell.Current.GoToAsync("//LoginPage");
            return;
        }

        _viewModel.LoadDashboard();
    }

    private async void OnAddCategoryClicked(object sender, EventArgs e)
    {
        await _viewModel.AddCategoryAsync();
    }

    private async void OnAddExpenseClicked(object sender, EventArgs e)
    {
        if ((sender as Button)?.CommandParameter is CategoryCardViewModel category)
        {
            await _viewModel.AddExpenseAsync(category);
        }
    }

    private async void OnDeleteCategoryClicked(object sender, EventArgs e)
    {
        if ((sender as Button)?.CommandParameter is CategoryCardViewModel category)
        {
            await _viewModel.DeleteCategoryAsync(category);
        }
    }

    private async void OnSetAlertThresholdClicked(object sender, EventArgs e)
    {
        if ((sender as Button)?.CommandParameter is CategoryCardViewModel category)
        {
            await _viewModel.SetAlertThresholdAsync(category);
        }
    }

    private async void OnDeleteExpenseClicked(object sender, EventArgs e)
    {
        if ((sender as Button)?.CommandParameter is ExpenseRowViewModel expense)
        {
            await _viewModel.DeleteExpenseAsync(expense);
        }
    }

    private async void OnAddGoalClicked(object sender, EventArgs e)
    {
        await _viewModel.AddGoalAsync();
    }

    private async void OnDeleteGoalClicked(object sender, EventArgs e)
    {
        if ((sender as Button)?.CommandParameter is GoalCardViewModel goal)
        {
            await _viewModel.DeleteGoalAsync(goal);
        }
    }

    private async void OnProfileClicked(object sender, EventArgs e)
    {
        await _viewModel.OpenProfileAsync();
    }

    private async void OnFamilyClicked(object sender, EventArgs e)
    {
        await _viewModel.OpenFamilyAsync();
    }

    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        await _viewModel.LogoutAsync();
    }

    private void OnExpenseFilterChanged(object? sender, EventArgs e)
    {
        _viewModel.RefreshExpenseInsights();
    }
}
