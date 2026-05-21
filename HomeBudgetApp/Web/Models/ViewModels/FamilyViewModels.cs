using SharedData;

namespace Web.Models.ViewModels;

public sealed class FamilyPageViewModel
{
    public bool HasFamily { get; init; }
    public string FamilyName { get; init; } = "";
    public string CurrentUserRole { get; init; } = "";
    public bool CanManageMembers { get; init; }
    public bool CanDeleteFamily { get; init; }
    public IReadOnlyList<FamilyInvitationViewModel> PendingInvitations { get; init; } = Array.Empty<FamilyInvitationViewModel>();
    public IReadOnlyList<FamilyMemberViewModel> Members { get; init; } = Array.Empty<FamilyMemberViewModel>();
    public IReadOnlyList<FamilyExpenseRowViewModel> ExpenseRows { get; init; } = Array.Empty<FamilyExpenseRowViewModel>();
    public IReadOnlyList<FamilyBudgetSummaryViewModel> BudgetSummaries { get; init; } = Array.Empty<FamilyBudgetSummaryViewModel>();
    public IReadOnlyList<FamilyMemberFilterOptionViewModel> MemberFilterOptions { get; init; } = Array.Empty<FamilyMemberFilterOptionViewModel>();
    public IReadOnlyList<FamilyCategoryFilterOptionViewModel> CategoryFilterOptions { get; init; } = Array.Empty<FamilyCategoryFilterOptionViewModel>();
    public int? SelectedMemberUserId { get; init; }
    public string? SelectedCategoryName { get; init; }
    public bool UseStartDate { get; init; }
    public bool UseEndDate { get; init; }
    public DateTime StartDate { get; init; } = DateTime.Today.AddMonths(-1);
    public DateTime EndDate { get; init; } = DateTime.Today;
    public string ExpensesCaption { get; init; } = "";
    public string BudgetOverviewCaption { get; init; } = "";
    public double BudgetOverviewTotal { get; init; }
}

public sealed class FamilyInvitationViewModel
{
    public FamilyInvitationViewModel(FamilyInvitation invitation)
    {
        InvitationId = invitation.InvitationId;
        FamilyName = invitation.FamilyName;
        InvitedByDisplay = $"Invited by {invitation.InvitedByName}";
    }

    public int InvitationId { get; }
    public string FamilyName { get; }
    public string InvitedByDisplay { get; }
}

public sealed class FamilyMemberViewModel
{
    public FamilyMemberViewModel(int userId, string name, string email, string role, bool canRemove)
    {
        UserId = userId;
        Name = name;
        Email = email;
        Role = role;
        CanRemove = canRemove;
    }

    public int UserId { get; }
    public string Name { get; }
    public string Email { get; }
    public string Role { get; }
    public bool CanRemove { get; }
}

public sealed class FamilyExpenseRowViewModel
{
    public FamilyExpenseRowViewModel(FamilyExpenseRow row)
    {
        Description = row.Description;
        Amount = row.Amount;
        DetailDisplay = $"{row.MemberName} · {row.CategoryName} · {row.Date}";
    }

    public string Description { get; }
    public double Amount { get; }
    public string AmountDisplay => Amount.ToString("C");
    public string DetailDisplay { get; }
}

public sealed class FamilyBudgetSummaryViewModel
{
    public FamilyBudgetSummaryViewModel(FamilyBudgetCategorySummary summary)
    {
        CategoryName = summary.CategoryName;
        TotalSpent = summary.TotalSpent;
        DetailDisplay = $"{summary.ExpenseCount} expense{(summary.ExpenseCount == 1 ? "" : "s")} · {summary.MemberCount} member{(summary.MemberCount == 1 ? "" : "s")} · latest {summary.LatestExpenseDate}";
    }

    public string CategoryName { get; }
    public double TotalSpent { get; }
    public string TotalSpentDisplay => TotalSpent.ToString("C");
    public string DetailDisplay { get; }
}

public sealed class FamilyMemberFilterOptionViewModel
{
    public FamilyMemberFilterOptionViewModel(int? userId, string label)
    {
        UserId = userId;
        Label = label;
    }

    public int? UserId { get; }
    public string Label { get; }
}

public sealed class FamilyCategoryFilterOptionViewModel
{
    public FamilyCategoryFilterOptionViewModel(string? categoryName, string label)
    {
        CategoryName = categoryName;
        Label = label;
    }

    public string? CategoryName { get; }
    public string Label { get; }
}
