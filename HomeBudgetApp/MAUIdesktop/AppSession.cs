using SharedData;

namespace MAUIdesktop;

public static class AppSession
{
    public static User? CurrentUser { get; private set; }

    public static void SignIn(User user)
    {
        CurrentUser = user;
    }

    public static void RefreshUser(DatabaseRepository database)
    {
        if (CurrentUser is null)
        {
            return;
        }

        CurrentUser = database.GetUser(CurrentUser.UserId);
    }

    public static void SignOut()
    {
        CurrentUser = null;
    }
}
