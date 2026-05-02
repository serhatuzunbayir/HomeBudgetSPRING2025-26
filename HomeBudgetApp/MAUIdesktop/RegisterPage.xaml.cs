using SharedData;

namespace MAUIdesktop;

public partial class RegisterPage : ContentPage
{
    private readonly DatabaseRepository _db;

    public RegisterPage()
    {
        InitializeComponent();

        _db = AppDatabase.Instance;
    }

    private async void OnRegisterClicked(object sender, EventArgs e)
    {
        MessageLabel.TextColor = Colors.Red;

        var email = (EmailEntry.Text ?? "").Trim().ToLowerInvariant();
        var name = (NameEntry.Text ?? "").Trim();
        var password = PasswordEntry.Text ?? "";

        if (string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(name) ||
            string.IsNullOrWhiteSpace(password))
        {
            MessageLabel.Text = "All fields are required";
            return;
        }

        if (!email.Contains('@') || !email.Contains('.'))
        {
            MessageLabel.Text = "Enter a valid email";
            return;
        }

        if (password.Length < 6)
        {
            MessageLabel.Text = "Password must be at least 6 characters";
            return;
        }

        if (_db.GetUserByEmail(email) != null)
        {
            MessageLabel.Text = "User already exists";
            return;
        }

        _db.AddUser(email, name, password);

        MessageLabel.TextColor = Colors.Green;
        MessageLabel.Text = "Registered successfully";

        await Shell.Current.GoToAsync("..");
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}
