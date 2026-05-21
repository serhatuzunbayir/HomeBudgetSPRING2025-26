namespace MAUIdesktop;

public interface IAppNavigationService
{
    Task GoToAsync(string route);
}

public sealed class ShellNavigationService : IAppNavigationService
{
    public Task GoToAsync(string route)
    {
        return Shell.Current.GoToAsync(route);
    }
}
