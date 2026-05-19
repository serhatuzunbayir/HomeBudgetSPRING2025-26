
namespace SharedData;

public class User
{
    public int UserId { get; set; }
    public string Email { get; set; } = "";
    public string Name { get; set; } = "";
    public string Password { get; set; } = "";
}

public class Family
{
    public int FamilyId { get; set; }
    public string Name { get; set; } = "";
}

public class FamilyMember
{
    public int UserId { get; set; }
    public int FamilyId { get; set; }
    public string Role { get; set; } = "";
}

public class FamilyUser
{
    public int UserId { get; set; }
    public string Email { get; set; } = "";
    public string Name { get; set; } = "";
    public string Role { get; set; } = "";
}

public class FamilyInvitation
{
    public int InvitationId { get; set; }
    public int FamilyId { get; set; }
    public string FamilyName { get; set; } = "";
    public int InvitedByUserId { get; set; }
    public string InvitedByName { get; set; } = "";
    public string Status { get; set; } = "";
    public string CreatedAt { get; set; } = "";
}

public class Category
{
    public int CategoryId { get; set; }
    public int UserId { get; set; }
    public string Name { get; set; } = "";
    public double Budget { get; set; }
    public double? WarningThreshold { get; set; }

    public double RemainingBudget
    {
        get => Budget;
        set => Budget = value;
    }
}

public class Expense
{
    public int ExpenseId { get; set; }
    public int UserId { get; set; }
    public int CategoryId { get; set; }
    public string Description { get; set; } = "";
    public double Amount { get; set; }
    public string Date { get; set; } = "";
}

public enum GoalStatus
{
    Active,
    Reached,
    Missed
}

public class Goal
{
    public int GoalId { get; set; }
    public int UserId { get; set; }
    public string Name { get; set; } = "";
    public double TargetAmount { get; set; }
    public string CreatedAt { get; set; } = "";
    public string Deadline { get; set; } = "";
    public GoalStatus Status { get; set; } = GoalStatus.Active;
    public double CurrentSpent { get; set; }

    public double RemainingAmount => Math.Max(0, TargetAmount - CurrentSpent);
    public double ProgressRatio => TargetAmount <= 0 ? 1 : Math.Min(1, CurrentSpent / TargetAmount);
}

public class GoalEvent
{
    public int GoalEventId { get; set; }
    public int GoalId { get; set; }
    public int UserId { get; set; }
    public string GoalName { get; set; } = "";
    public GoalStatus Status { get; set; }
    public string Message { get; set; } = "";
    public string CreatedAt { get; set; } = "";
}

public class FamilyExpenseQuery
{
    public int? MemberUserId { get; set; }
    public string? CategoryName { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

public class FamilyExpenseRow
{
    public int ExpenseId { get; set; }
    public int FamilyId { get; set; }
    public int UserId { get; set; }
    public string MemberName { get; set; } = "";
    public string MemberEmail { get; set; } = "";
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = "";
    public string Description { get; set; } = "";
    public double Amount { get; set; }
    public string Date { get; set; } = "";
}

public class FamilyBudgetCategorySummary
{
    public string CategoryName { get; set; } = "";
    public double TotalSpent { get; set; }
    public int ExpenseCount { get; set; }
    public int MemberCount { get; set; }
    public string LatestExpenseDate { get; set; } = "";
}

public class FamilyCategory
{
    public int FamilyCategoryId { get; set; }
    public int FamilyId { get; set; }
    public string Name { get; set; } = "";
    public double Budget { get; set; }

    public double RemainingBudget
    {
        get => Budget;
        set => Budget = value;
    }
}

public class FamilyExpense
{
    public int FamilyExpenseId { get; set; }
    public int FamilyId { get; set; }
    public int UserId { get; set; }
    public int FamilyCategoryId { get; set; }
    public string Description { get; set; } = "";
    public double Amount { get; set; }
    public string Date { get; set; } = "";
}
