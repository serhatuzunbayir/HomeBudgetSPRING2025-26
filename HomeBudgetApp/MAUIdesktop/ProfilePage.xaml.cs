namespace MAUIdesktop;

public partial class ProfilePage : ContentPage
{
    private readonly ProfileViewModel _viewModel;

    public ProfilePage()
    {
        InitializeComponent();

        _viewModel = new ProfileViewModel(
            new DesktopAccountService(AppDatabase.Instance),
            new ShellNavigationService());
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadAsync();
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        await _viewModel.SaveAsync();
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await _viewModel.BackAsync();
    }
}
