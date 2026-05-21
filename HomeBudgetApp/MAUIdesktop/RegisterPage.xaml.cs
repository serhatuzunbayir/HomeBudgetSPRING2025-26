namespace MAUIdesktop;

public partial class RegisterPage : ContentPage
{
    private readonly RegisterViewModel _viewModel;

    public RegisterPage()
    {
        InitializeComponent();

        _viewModel = new RegisterViewModel(
            new DesktopAccountService(AppDatabase.Instance),
            new ShellNavigationService());
        BindingContext = _viewModel;
    }

    private async void OnRegisterClicked(object sender, EventArgs e)
    {
        await _viewModel.RegisterAsync(EmailEntry.Text, NameEntry.Text, PasswordEntry.Text);
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await _viewModel.BackAsync();
    }
}
