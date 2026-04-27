using SharedData;

namespace MAUIdesktop;

public partial class LoginPage : ContentPage
{
    private readonly DatabaseService _db;

    public LoginPage()
    {
        InitializeComponent();

        _db = AppDatabase.Instance;
    }

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        MessageLabel.TextColor = Colors.Red;

        var email = (EmailEntry.Text ?? "").Trim().ToLowerInvariant();
        var password = PasswordEntry.Text ?? "";

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            MessageLabel.Text = "Email and password are required";
            return;
        }

        var user = _db.GetUser(email);

        if (user == null)
        {
            MessageLabel.Text = "User not found";
            return;
        }

        if (user.Password != password)
        {
            MessageLabel.Text = "Wrong password";
            return;
        }

        MessageLabel.TextColor = Colors.Green;
        MessageLabel.Text = $"Welcome {user.Name}";

        await Shell.Current.GoToAsync("//MainPage");
    }

    private async void OnGoRegisterClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(RegisterPage));
    }
}
