using Microsoft.AspNetCore.Mvc;
using SharedData;
using Web.Models.ViewModels;

namespace Web.Controllers;

public sealed class AccountController : Controller
{
    private const string UserIdSessionKey = AuthenticatedController.UserIdSessionKey;
    private readonly IDatabaseRepository _database;

    public AccountController(IDatabaseRepository database)
    {
        _database = database;
    }

    [HttpGet]
    public IActionResult Login()
    {
        return View(new LoginViewModel());
    }

    [HttpPost]
    public IActionResult Login(LoginViewModel model)
    {
        var email = (model.Email ?? "").Trim().ToLowerInvariant();
        var password = model.Password ?? "";

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            TempData["ErrorMessage"] = "Email and password are required.";
            model.Password = "";
            return View(model);
        }

        var user = _database.GetUserByEmail(email);
        if (user is null || user.Password != password)
        {
            TempData["ErrorMessage"] = user is null ? "User not found." : "Wrong password.";
            model.Password = "";
            return View(model);
        }

        HttpContext.Session.SetInt32(UserIdSessionKey, user.UserId);
        TempData["SuccessMessage"] = $"Welcome {user.Name}.";
        return RedirectToAction("Index", "Dashboard");
    }

    [HttpGet]
    public IActionResult Register()
    {
        return View(new RegisterViewModel());
    }

    [HttpPost]
    public IActionResult Register(RegisterViewModel model)
    {
        var email = (model.Email ?? "").Trim().ToLowerInvariant();
        var name = (model.Name ?? "").Trim();
        var password = model.Password ?? "";

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(password))
        {
            TempData["ErrorMessage"] = "All fields are required.";
            model.Password = "";
            return View(model);
        }

        if (!email.Contains('@') || !email.Contains('.'))
        {
            TempData["ErrorMessage"] = "Enter a valid email.";
            model.Password = "";
            return View(model);
        }

        if (password.Length < 6)
        {
            TempData["ErrorMessage"] = "Password must be at least 6 characters.";
            model.Password = "";
            return View(model);
        }

        if (_database.GetUserByEmail(email) is not null)
        {
            TempData["ErrorMessage"] = "User already exists.";
            model.Password = "";
            return View(model);
        }

        _database.AddUser(email, name, password);
        TempData["SuccessMessage"] = "Registered successfully. You can now log in.";
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult Profile()
    {
        var userId = HttpContext.Session.GetInt32(UserIdSessionKey);
        if (!userId.HasValue)
        {
            return RedirectToAction(nameof(Login));
        }

        var user = _database.GetUser(userId.Value);
        if (user is null)
        {
            HttpContext.Session.Clear();
            return RedirectToAction(nameof(Login));
        }

        return View(new ProfileViewModel
        {
            Email = user.Email,
            Name = user.Name
        });
    }

    [HttpPost]
    public IActionResult Profile(ProfileViewModel model)
    {
        var userId = HttpContext.Session.GetInt32(UserIdSessionKey);
        if (!userId.HasValue)
        {
            return RedirectToAction(nameof(Login));
        }

        var user = _database.GetUser(userId.Value);
        if (user is null)
        {
            HttpContext.Session.Clear();
            return RedirectToAction(nameof(Login));
        }

        var name = (model.Name ?? "").Trim();
        var password = model.Password ?? "";
        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["ErrorMessage"] = "Name is required.";
            return View(new ProfileViewModel { Email = user.Email, Name = name });
        }

        if (!string.IsNullOrWhiteSpace(password) && password.Length < 6)
        {
            TempData["ErrorMessage"] = "Password must be at least 6 characters.";
            return View(new ProfileViewModel { Email = user.Email, Name = name });
        }

        _database.UpdateUser(user.UserId, name, string.IsNullOrWhiteSpace(password) ? user.Password : password);
        TempData["SuccessMessage"] = "Profile updated.";
        return RedirectToAction("Index", "Dashboard");
    }

    [HttpPost]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        TempData["SuccessMessage"] = "Logged out.";
        return RedirectToAction(nameof(Login));
    }
}
