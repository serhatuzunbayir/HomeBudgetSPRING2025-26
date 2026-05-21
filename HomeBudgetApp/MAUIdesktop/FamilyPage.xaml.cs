namespace MAUIdesktop;

public partial class FamilyPage : ContentPage
{
    private readonly FamilyViewModel _viewModel;

    public FamilyPage()
    {
        InitializeComponent();

        _viewModel = new FamilyViewModel(
            AppDatabase.Instance,
            new PageDialogService(this),
            new ShellNavigationService());
        BindingContext = _viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (!_viewModel.IsSignedIn)
        {
            Shell.Current.GoToAsync("//LoginPage");
            return;
        }

        _viewModel.LoadFamily();
    }

    private async void OnPersonalClicked(object sender, EventArgs e)
    {
        await _viewModel.OpenPersonalAsync();
    }

    private async void OnCreateFamilyClicked(object sender, EventArgs e)
    {
        await _viewModel.CreateFamilyAsync();
    }

    private async void OnInviteMemberClicked(object sender, EventArgs e)
    {
        await _viewModel.InviteMemberAsync();
    }

    private async void OnDeleteFamilyClicked(object sender, EventArgs e)
    {
        await _viewModel.DeleteFamilyAsync();
    }

    private async void OnRemoveMemberClicked(object sender, EventArgs e)
    {
        if ((sender as Button)?.CommandParameter is FamilyMemberViewModel member)
        {
            await _viewModel.RemoveMemberAsync(member);
        }
    }

    private async void OnAcceptInvitationClicked(object sender, EventArgs e)
    {
        if ((sender as Button)?.CommandParameter is FamilyInvitationViewModel invitation)
        {
            await _viewModel.AcceptInvitationAsync(invitation);
        }
    }

    private void OnDeclineInvitationClicked(object sender, EventArgs e)
    {
        if ((sender as Button)?.CommandParameter is FamilyInvitationViewModel invitation)
        {
            _viewModel.DeclineInvitation(invitation);
        }
    }

    private void OnFamilyDateFilterChanged(object sender, CheckedChangedEventArgs e)
    {
        _viewModel.RefreshFamilyExpenseRows();
    }

    private void OnFamilyDateSelected(object sender, DateChangedEventArgs e)
    {
        _viewModel.RefreshFamilyExpenseRows();
    }

    private async void OnApplyFamilyExpenseFiltersClicked(object sender, EventArgs e)
    {
        await _viewModel.ApplyFamilyExpenseFiltersAsync();
    }

    private void OnClearFamilyExpenseFiltersClicked(object sender, EventArgs e)
    {
        _viewModel.ClearFamilyExpenseFilters();
    }
}
