namespace MAUIdesktop;

public interface IUserDialogService
{
    Task<string?> PromptAsync(string title, string message, string accept, string cancel, string placeholder = "", Keyboard? keyboard = null);
    Task AlertAsync(string title, string message, string cancel = "OK");
    Task<bool> ConfirmAsync(string title, string message, string accept, string cancel);
}

public sealed class PageDialogService : IUserDialogService
{
    private readonly Page _page;

    public PageDialogService(Page page)
    {
        _page = page;
    }

    public Task<string?> PromptAsync(string title, string message, string accept, string cancel, string placeholder = "", Keyboard? keyboard = null)
    {
        return _page.DisplayPromptAsync(title, message, accept, cancel, placeholder, keyboard: keyboard);
    }

    public Task AlertAsync(string title, string message, string cancel = "OK")
    {
        return _page.DisplayAlert(title, message, cancel);
    }

    public Task<bool> ConfirmAsync(string title, string message, string accept, string cancel)
    {
        return _page.DisplayAlert(title, message, accept, cancel);
    }
}
