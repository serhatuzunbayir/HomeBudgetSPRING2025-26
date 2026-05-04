namespace SharedData;

public sealed class BudgetAlertNotification
{
    public int CategoryId { get; init; }
    public string CategoryName { get; init; } = "";
    public double RemainingBudget { get; init; }
    public double WarningThreshold { get; init; }

    public bool IsOverBudget => RemainingBudget < 0;

    public string Title => IsOverBudget ? "Budget exceeded" : "Budget alert";

    public string Message => IsOverBudget
        ? $"{CategoryName} is over budget by {Math.Abs(RemainingBudget):C}. Your alert threshold is {WarningThreshold:C}."
        : $"{CategoryName} has {RemainingBudget:C} remaining, which is below your alert threshold of {WarningThreshold:C}.";
}

// The desktop page passes a UI method through this delegate when an alert must be shown.
public delegate Task BudgetAlertRaisedHandler(BudgetAlertNotification notification);

public static class BudgetAlertService
{
    // Finds all categories that are already in an alert state.
    public static IReadOnlyList<BudgetAlertNotification> GetActiveAlerts(IEnumerable<Category> categories)
    {
        return categories
            .Where(IsAlertActive)
            .Select(CreateNotification)
            .OrderByDescending(notification => notification.IsOverBudget)
            .ThenBy(notification => notification.RemainingBudget)
            .ToList();
    }

    public static async Task NotifyIfThresholdCrossedAsync(
        Category previousCategory,
        Category currentCategory,
        BudgetAlertRaisedHandler onAlertRaised)
    {
        // This delegate is only called when the category crosses into the alert zone.
        var notification = EvaluateThresholdCrossing(previousCategory, currentCategory);
        if (notification is null)
        {
            return;
        }

        await onAlertRaised(notification);
    }

    public static BudgetAlertNotification? EvaluateThresholdCrossing(Category previousCategory, Category currentCategory)
    {
        if (!currentCategory.WarningThreshold.HasValue)
        {
            return null;
        }

        var wasAlerting = IsAlertActive(previousCategory);
        var isAlerting = IsAlertActive(currentCategory);

        return !wasAlerting && isAlerting
            ? CreateNotification(currentCategory)
            : null;
    }

    public static bool IsAlertActive(Category category)
    {
        return category.RemainingBudget < 0 ||
               (category.WarningThreshold.HasValue &&
                category.RemainingBudget < category.WarningThreshold.Value);
    }

    private static BudgetAlertNotification CreateNotification(Category category)
    {
        return new BudgetAlertNotification
        {
            CategoryId = category.CategoryId,
            CategoryName = category.Name,
            RemainingBudget = category.RemainingBudget,
            WarningThreshold = category.WarningThreshold ?? 0
        };
    }
}
