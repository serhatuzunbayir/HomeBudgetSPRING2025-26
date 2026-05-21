namespace MAUIdesktop;

public sealed class LoginViewModel : ObservableObject
{
    private readonly DesktopAccountService _accounts;
    private readonly IAppNavigationService _navigation;
    private string _message = "";
    private Color _messageColor = Colors.Red;

    public LoginViewModel(DesktopAccountService accounts, IAppNavigationService navigation)
    {
        _accounts = accounts;
        _navigation = navigation;
    }

    public string Message { get => _message; private set => SetProperty(ref _message, value); }
    public Color MessageColor { get => _messageColor; private set => SetProperty(ref _messageColor, value); }

    public async Task LoginAsync(string? rawEmail, string? rawPassword)
    {
        MessageColor = Colors.Red;
        var email = (rawEmail ?? "").Trim().ToLowerInvariant();
        var password = rawPassword ?? "";

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            Message = "Email and password are required";
            return;
        }

        var user = _accounts.Login(email, password);
        if (user is null)
        {
            Message = _accounts.UserExists(email) ? "Wrong password" : "User not found";
            return;
        }

        MessageColor = Colors.Green;
        Message = $"Welcome {user.Name}";
        AppSession.SignIn(user);
        await _navigation.GoToAsync("//MainPage");
    }

    public Task OpenRegisterAsync() => _navigation.GoToAsync(nameof(RegisterPage));
}
