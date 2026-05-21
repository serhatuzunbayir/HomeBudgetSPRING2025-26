using Microsoft.AspNetCore.Mvc;
using SharedData;

namespace Web.Controllers;

public sealed class CategoryController : AuthenticatedController
{
    public CategoryController(IDatabaseRepository database)
        : base(database)
    {
    }

    [HttpPost]
    public IActionResult Add(string name, string budget, string? warningThreshold)
    {
        if (!TryGetCurrentUser(out var user))
        {
            return RedirectToLogin();
        }

        name = (name ?? "").Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            ShowError("Category name is required.");
            return RedirectToAction("Index", "Dashboard");
        }

        if (!TryReadMoney(budget, out var parsedBudget) || parsedBudget < 0)
        {
            ShowError("Please enter a valid positive budget.");
            return RedirectToAction("Index", "Dashboard");
        }

        if (!TryReadOptionalMoney(warningThreshold, out var parsedThreshold) || parsedThreshold < 0)
        {
            ShowError("Enter a valid positive threshold or leave it blank.");
            return RedirectToAction("Index", "Dashboard");
        }

        try
        {
            Database.AddCategory(user.UserId, name, parsedBudget, parsedThreshold);
            ShowSuccess("Category added.");
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }

        return RedirectToAction("Index", "Dashboard");
    }

    [HttpPost]
    public IActionResult UpdateAlert(int categoryId, string? warningThreshold)
    {
        if (!TryGetCurrentUser(out var user))
        {
            return RedirectToLogin();
        }

        var category = Database.GetCategory(categoryId, user.UserId);
        if (category is null)
        {
            ShowError("Category was not found.");
            return RedirectToAction("Index", "Dashboard");
        }

        if (!TryReadOptionalMoney(warningThreshold, out var parsedThreshold) || parsedThreshold < 0)
        {
            ShowError("Enter a valid positive threshold or leave it blank.");
            return RedirectToAction("Index", "Dashboard");
        }

        Database.UpdateCategory(category.CategoryId, user.UserId, category.Name, category.RemainingBudget, parsedThreshold);
        ShowSuccess("Budget alert updated.");
        return RedirectToAction("Index", "Dashboard");
    }

    [HttpPost]
    public IActionResult Delete(int categoryId)
    {
        if (!TryGetCurrentUser(out var user))
        {
            return RedirectToLogin();
        }

        Database.DeleteCategory(categoryId, user.UserId);
        ShowSuccess("Category deleted.");
        return RedirectToAction("Index", "Dashboard");
    }
}
