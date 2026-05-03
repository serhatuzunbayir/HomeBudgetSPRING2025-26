
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
