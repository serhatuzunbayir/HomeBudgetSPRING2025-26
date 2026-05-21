using Microsoft.AspNetCore.Mvc;
using SharedData;

namespace Web.Controllers;

public sealed class GoalController : AuthenticatedController
{
    public GoalController(IDatabaseRepository database)
        : base(database)
    {
    }

    [HttpPost]
    public IActionResult Add(string name, string targetAmount, DateTime deadline)
    {
        if (!TryGetCurrentUser(out var user))
        {
            return RedirectToLogin();
        }

        name = (name ?? "").Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            ShowError("Goal name is required.");
            return RedirectToAction("Index", "Dashboard");
        }

        if (!TryReadMoney(targetAmount, out var parsedTargetAmount) || parsedTargetAmount <= 0)
        {
            ShowError("Please enter a spending cap greater than zero.");
            return RedirectToAction("Index", "Dashboard");
        }

        Database.AddGoal(user.UserId, name, parsedTargetAmount, deadline);
        ShowSuccess("Goal added.");
        return RedirectToAction("Index", "Dashboard");
    }

    [HttpPost]
    public IActionResult Delete(int goalId)
    {
        if (!TryGetCurrentUser(out var user))
        {
            return RedirectToLogin();
        }

        Database.DeleteGoal(goalId, user.UserId);
        ShowSuccess("Goal deleted.");
        return RedirectToAction("Index", "Dashboard");
    }
}
