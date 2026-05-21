namespace MAUIdesktop;

public sealed class RegisterViewModel : ObservableObject
{
    private readonly DesktopAccountService _accounts;
    private readonly IAppNavigationService _navigation;
    private string _message = "";
    private Color _messageColor = Colors.Red;

    public RegisterViewModel(DesktopAccountService accounts, IAppNavigationService navigation)
    {
        _accounts = accounts;
        _navigation = navigation;
    }

    public string Message { get => _message; private set => SetProperty(ref _message, value); }
    public Color MessageColor { get => _messageColor; private set => SetProperty(ref _messageColor, value); }

    public async Task RegisterAsync(string? rawEmail, string? rawName, string? rawPassword)
    {
        MessageColor = Colors.Red;
        var email = (rawEmail ?? "").Trim().ToLowerInvariant();
        var name = (rawName ?? "").Trim();
        var password = rawPassword ?? "";

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(password))
        {
            Message = "All fields are required";
            return;
        }

        if (!email.Contains('@') || !email.Contains('.'))
        {
            Message = "Enter a valid email";
            return;
        }

        if (password.Length < 6)
        {
            Message = "Password must be at least 6 characters";
            return;
        }

        if (_accounts.UserExists(email))
        {
            Message = "User already exists";
            return;
        }

        _accounts.Register(email, name, password);
        MessageColor = Colors.Green;
        Message = "Registered successfully";
        await _navigation.GoToAsync("..");
    }

    public Task BackAsync() => _navigation.GoToAsync("..");
}
