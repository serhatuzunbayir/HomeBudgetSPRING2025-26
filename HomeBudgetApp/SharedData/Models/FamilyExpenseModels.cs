namespace SharedData;

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
