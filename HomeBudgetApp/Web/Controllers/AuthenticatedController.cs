using Microsoft.AspNetCore.Mvc;
using SharedData;

namespace Web.Controllers;

public abstract class AuthenticatedController : Controller
{
    public const string UserIdSessionKey = "UserId";
    protected readonly IDatabaseRepository Database;

    protected AuthenticatedController(IDatabaseRepository database)
    {
        Database = database;
    }

    protected int? CurrentUserId => HttpContext.Session.GetInt32(UserIdSessionKey);

    protected User? CurrentUser => CurrentUserId.HasValue
        ? Database.GetUser(CurrentUserId.Value)
        : null;

    protected IActionResult RedirectToLogin()
    {
        return RedirectToAction("Login", "Account");
    }

    protected bool TryGetCurrentUser(out User user)
    {
        user = CurrentUser!;
        return user is not null;
    }

    protected static bool TryReadMoney(string? value, out double amount)
    {
        value = (value ?? "").Trim().Replace(',', '.');
        return double.TryParse(
            value,
            System.Globalization.NumberStyles.Number,
            System.Globalization.CultureInfo.InvariantCulture,
            out amount);
    }

    protected static bool TryReadOptionalMoney(string? value, out double? amount)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            amount = null;
            return true;
        }

        if (TryReadMoney(value, out var parsedAmount))
        {
            amount = parsedAmount;
            return true;
        }

        amount = null;
        return false;
    }

    protected void ShowSuccess(string message)
    {
        TempData["SuccessMessage"] = message;
    }

    protected void ShowError(string message)
    {
        TempData["ErrorMessage"] = message;
    }
}
