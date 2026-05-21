using SharedData;

namespace MAUIdesktop;

public sealed class DesktopAccountService
{
    private readonly IDatabaseRepository _database;

    public DesktopAccountService(IDatabaseRepository database)
    {
        _database = database;
    }

    public User? Login(string email, string password)
    {
        var user = _database.GetUserByEmail(email);
        return user is not null && user.Password == password ? user : null;
    }

    public bool UserExists(string email)
    {
        return _database.GetUserByEmail(email) is not null;
    }

    public void Register(string email, string name, string password)
    {
        _database.AddUser(email, name, password);
    }

    public User? RefreshCurrentUser()
    {
        AppSession.RefreshUser(_database);
        return AppSession.CurrentUser;
    }

    public void UpdateCurrentUser(string name, string password)
    {
        var user = AppSession.CurrentUser;
        if (user is null)
        {
            return;
        }

        _database.UpdateUser(user.UserId, name, string.IsNullOrWhiteSpace(password) ? user.Password : password);
        AppSession.RefreshUser(_database);
    }
}
