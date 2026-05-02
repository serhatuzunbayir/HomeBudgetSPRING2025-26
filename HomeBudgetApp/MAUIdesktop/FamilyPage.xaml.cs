namespace MAUIdesktop;

public partial class FamilyPage : ContentPage
{
    public FamilyPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (AppSession.CurrentUser is null)
        {
            Shell.Current.GoToAsync("//LoginPage");
        }
    }

    private async void OnPersonalClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//MainPage");
    }
}
