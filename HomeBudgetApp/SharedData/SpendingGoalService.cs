using System.Globalization;

namespace SharedData;

public static class SpendingGoalService
{
    // Decides whether a spending cap goal is still active, reached, or missed.
    public static GoalStatus EvaluateBudgetCapGoal(Goal goal, double currentSpent, DateTime asOf)
    {
        if (currentSpent > goal.TargetAmount)
        {
            return GoalStatus.Missed;
        }

        return asOf.Date >= ParseDate(goal.Deadline).Date
            ? GoalStatus.Reached
            : GoalStatus.Active;
    }

    // Creates the text shown when a goal changes status.
    public static string BuildEventMessage(Goal goal, GoalStatus status)
    {
        return status switch
        {
            GoalStatus.Reached => $"{goal.Name} reached: spending stayed at {goal.CurrentSpent:C} against a {goal.TargetAmount:C} cap by {goal.Deadline}.",
            GoalStatus.Missed => $"{goal.Name} missed: spending reached {goal.CurrentSpent:C}, above the {goal.TargetAmount:C} cap.",
            _ => $"{goal.Name} is still active."
        };
    }

    // Reads database date strings while keeping the expected yyyy-MM-dd format first.
    public static DateTime ParseDate(string value)
    {
        return DateTime.TryParseExact(
            value,
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var exactDate)
            ? exactDate
            : DateTime.Parse(value, CultureInfo.InvariantCulture);
    }

    // Stores dates in the same format used by the database and UI.
    public static string FormatDate(DateTime value)
    {
        return value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }
}
