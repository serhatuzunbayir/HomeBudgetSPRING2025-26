using Microsoft.AspNetCore.Mvc;
using SharedData;

namespace Web.Controllers;

public sealed class ExpenseController : AuthenticatedController
{
    public ExpenseController(IDatabaseRepository database)
        : base(database)
    {
    }

    [HttpPost]
    public IActionResult Add(int categoryId, string description, string amount)
    {
        if (!TryGetCurrentUser(out var user))
        {
            return RedirectToLogin();
        }

        description = (description ?? "").Trim();
        if (string.IsNullOrWhiteSpace(description))
        {
            ShowError("Expense description is required.");
            return RedirectToAction("Index", "Dashboard");
        }

        if (!TryReadMoney(amount, out var parsedAmount) || parsedAmount <= 0)
        {
            ShowError("Please enter a valid amount greater than zero.");
            return RedirectToAction("Index", "Dashboard");
        }

        if (Database.GetCategory(categoryId, user.UserId) is null)
        {
            ShowError("Category was not found.");
            return RedirectToAction("Index", "Dashboard");
        }

        Database.AddExpense(user.UserId, categoryId, description, parsedAmount, DateTime.Now.ToString("yyyy-MM-dd"));
        ShowSuccess("Expense added.");
        return RedirectToAction("Index", "Dashboard");
    }

    [HttpPost]
    public IActionResult Delete(int expenseId)
    {
        if (!TryGetCurrentUser(out var user))
        {
            return RedirectToLogin();
        }

        Database.DeleteExpense(expenseId, user.UserId);
        ShowSuccess("Expense deleted.");
        return RedirectToAction("Index", "Dashboard");
    }
}
