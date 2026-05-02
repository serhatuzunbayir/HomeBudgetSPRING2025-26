using SharedData;

namespace MAUIdesktop;

public partial class ProfilePage : ContentPage
{
    private readonly DatabaseRepository _db;

    public ProfilePage()
    {
        InitializeComponent();
        _db = AppDatabase.Instance;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        LoadProfile();
    }

    private void LoadProfile()
    {
        AppSession.RefreshUser(_db);
        var user = AppSession.CurrentUser;

        if (user is null)
        {
            Shell.Current.GoToAsync("//LoginPage");
            return;
        }

        EmailLabel.Text = user.Email;
        NameEntry.Text = user.Name;
        PasswordEntry.Text = "";
        MessageLabel.Text = "";
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        var user = AppSession.CurrentUser;
        if (user is null)
        {
            return;
        }

        var name = (NameEntry.Text ?? "").Trim();
        var password = PasswordEntry.Text ?? "";

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

        _db.UpdateUser(user.UserId, name, string.IsNullOrWhiteSpace(password) ? user.Password : password);
        AppSession.RefreshUser(_db);

        ShowMessage("Profile updated.", Colors.Green);
        await Task.Delay(500);
        await Shell.Current.GoToAsync("..");
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }

    private void ShowMessage(string message, Color color)
    {
        MessageLabel.Text = message;
        MessageLabel.TextColor = color;
    }
}
