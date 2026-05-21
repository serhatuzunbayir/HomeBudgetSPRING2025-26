namespace MAUIdesktop;

public sealed class ProfileViewModel : ObservableObject
{
    private readonly DesktopAccountService _accounts;
    private readonly IAppNavigationService _navigation;
    private string _email = "";
    private string _name = "";
    private string _password = "";
    private string _message = "";
    private Color _messageColor = Colors.Red;

    public ProfileViewModel(DesktopAccountService accounts, IAppNavigationService navigation)
    {
        _accounts = accounts;
        _navigation = navigation;
    }

    public string Email { get => _email; private set => SetProperty(ref _email, value); }
    public string Name { get => _name; set => SetProperty(ref _name, value); }
    public string Password { get => _password; set => SetProperty(ref _password, value); }
    public string Message { get => _message; private set => SetProperty(ref _message, value); }
    public Color MessageColor { get => _messageColor; private set => SetProperty(ref _messageColor, value); }

    public async Task LoadAsync()
    {
        var user = _accounts.RefreshCurrentUser();
        if (user is null)
        {
            await _navigation.GoToAsync("//LoginPage");
            return;
        }

        Email = user.Email;
        Name = user.Name;
        Password = "";
        Message = "";
    }

    public async Task SaveAsync()
    {
        var name = (Name ?? "").Trim();
        var password = Password ?? "";

        if (string.IsNullOrWhiteSpace(name))
        {
            ShowMessage("Name is required.", Colors.Red);
            return;
        }

        if (!string.IsNullOrWhiteSpace(password) && password.Length < 6)
        {
            ShowMessage("Password must be at least 6 characters.", Colors.Red);
            return;
        }

        _accounts.UpdateCurrentUser(name, password);
        ShowMessage("Profile updated.", Colors.Green);
        await Task.Delay(500);
        await _navigation.GoToAsync("..");
    }

    public Task BackAsync() => _navigation.GoToAsync("..");

    private void ShowMessage(string message, Color color)
    {
        Message = message;
        MessageColor = color;
    }
}
