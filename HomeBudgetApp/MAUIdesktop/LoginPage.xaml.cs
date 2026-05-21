namespace MAUIdesktop;

public partial class LoginPage : ContentPage
{
    private readonly LoginViewModel _viewModel;

    public LoginPage()
    {
        InitializeComponent();

        _viewModel = new LoginViewModel(
            new DesktopAccountService(AppDatabase.Instance),
            new ShellNavigationService());
        BindingContext = _viewModel;
    }

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        await _viewModel.LoginAsync(EmailEntry.Text, PasswordEntry.Text);
    }

    private async void OnGoRegisterClicked(object sender, EventArgs e)
    {
        await _viewModel.OpenRegisterAsync();
    }
}
