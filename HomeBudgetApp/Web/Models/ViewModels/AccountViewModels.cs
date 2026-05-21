namespace Web.Models.ViewModels;

public sealed class LoginViewModel
{
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
}

public sealed class RegisterViewModel
{
    public string Email { get; set; } = "";
    public string Name { get; set; } = "";
    public string Password { get; set; } = "";
}

public sealed class ProfileViewModel
{
    public string Email { get; init; } = "";
    public string Name { get; set; } = "";
    public string Password { get; set; } = "";
}
