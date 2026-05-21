using Microsoft.AspNetCore.Mvc;
using SharedData;
using Web.Models.ViewModels;

namespace Web.Controllers;

public sealed class FamilyController : AuthenticatedController
{
    public FamilyController(IDatabaseRepository database)
        : base(database)
    {
    }

    [HttpGet]
    public IActionResult Index(
        int? memberUserId,
        string? categoryName,
        bool useStartDate = false,
        bool useEndDate = false,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        if (!TryGetCurrentUser(out var user))
        {
            return RedirectToLogin();
        }

        var family = Database.GetFamilyByUser(user.UserId);
        if (family is null)
        {
            return View(new FamilyPageViewModel
            {
                HasFamily = false,
                PendingInvitations = Database.GetPendingFamilyInvitations(user.UserId)
                    .Select(invitation => new FamilyInvitationViewModel(invitation))
                    .ToList()
            });
        }

        var members = Database.GetFamilyUsers(family.FamilyId);
        var currentMember = members.FirstOrDefault(member => member.UserId == user.UserId);
        var canManage = IsFamilyAdmin(currentMember?.Role);
        var canDelete = IsFamilyOwner(currentMember?.Role);
        var filterStartDate = useStartDate ? startDate ?? DateTime.Today.AddMonths(-1) : (DateTime?)null;
        var filterEndDate = useEndDate ? endDate ?? DateTime.Today : (DateTime?)null;

        if (filterStartDate.HasValue && filterEndDate.HasValue && filterStartDate.Value.Date > filterEndDate.Value.Date)
        {
            ShowError("Start date must be on or before end date.");
            filterStartDate = null;
            filterEndDate = null;
            useStartDate = false;
            useEndDate = false;
        }

        var query = new FamilyExpenseQuery
        {
            MemberUserId = memberUserId,
            CategoryName = string.IsNullOrWhiteSpace(categoryName) ? null : categoryName.Trim(),
            StartDate = filterStartDate,
            EndDate = filterEndDate
        };
        var expenseRows = Database.GetFamilyExpenseRows(family.FamilyId, query);
        var summaries = canManage
            ? Database.GetFamilyBudgetOverview(family.FamilyId)
            : new List<FamilyBudgetCategorySummary>();
        var total = expenseRows.Sum(row => row.Amount);

        var model = new FamilyPageViewModel
        {
            HasFamily = true,
            FamilyName = family.Name,
            CurrentUserRole = currentMember?.Role ?? "Member",
            CanManageMembers = canManage,
            CanDeleteFamily = canDelete,
            Members = members
                .Select(member => new FamilyMemberViewModel(
                    member.UserId,
                    member.Name,
                    member.Email,
                    member.Role,
                    canManage && member.UserId != user.UserId))
                .ToList(),
            ExpenseRows = expenseRows.Select(row => new FamilyExpenseRowViewModel(row)).ToList(),
            BudgetSummaries = summaries.Select(summary => new FamilyBudgetSummaryViewModel(summary)).ToList(),
            MemberFilterOptions = new[] { new FamilyMemberFilterOptionViewModel(null, "All members") }
                .Concat(members.Select(member => new FamilyMemberFilterOptionViewModel(member.UserId, member.Name)))
                .ToList(),
            CategoryFilterOptions = new[] { new FamilyCategoryFilterOptionViewModel(null, "All categories") }
                .Concat(Database.GetFamilyCategoryNames(family.FamilyId)
                    .Select(name => new FamilyCategoryFilterOptionViewModel(name, name)))
                .ToList(),
            SelectedMemberUserId = memberUserId,
            SelectedCategoryName = string.IsNullOrWhiteSpace(categoryName) ? null : categoryName.Trim(),
            UseStartDate = useStartDate,
            UseEndDate = useEndDate,
            StartDate = startDate ?? DateTime.Today.AddMonths(-1),
            EndDate = endDate ?? DateTime.Today,
            ExpensesCaption = expenseRows.Count == 0
                ? "No family expenses match the selected filters."
                : $"{expenseRows.Count} expense{(expenseRows.Count == 1 ? "" : "s")} · total {total:C}",
            BudgetOverviewCaption = !canManage
                ? "Only family owners and admins can view combined family totals."
                : summaries.Count == 0
                    ? "No family spending has been recorded yet."
                    : $"{summaries.Count} categor{(summaries.Count == 1 ? "y" : "ies")} across all family members.",
            BudgetOverviewTotal = summaries.Sum(summary => summary.TotalSpent)
        };

        return View(model);
    }

    [HttpPost]
    public IActionResult Create(string familyName)
    {
        if (!TryGetCurrentUser(out var user))
        {
            return RedirectToLogin();
        }

        if (Database.GetFamilyByUser(user.UserId) is not null)
        {
            ShowError("You already belong to a family.");
            return RedirectToAction(nameof(Index));
        }

        familyName = (familyName ?? "").Trim();
        if (string.IsNullOrWhiteSpace(familyName))
        {
            ShowError("Family name is required.");
            return RedirectToAction(nameof(Index));
        }

        Database.CreateFamily(user.UserId, familyName, "Owner");
        ShowSuccess("Family created.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public IActionResult Delete(string confirmationName)
    {
        if (!TryGetCurrentUser(out var user))
        {
            return RedirectToLogin();
        }

        var family = Database.GetFamilyByUser(user.UserId);
        if (family is null || !IsFamilyOwner(Database.GetFamilyMemberRole(family.FamilyId, user.UserId)))
        {
            ShowError("Only the family owner can delete the family workspace.");
            return RedirectToAction(nameof(Index));
        }

        if (!string.Equals((confirmationName ?? "").Trim(), family.Name, StringComparison.Ordinal))
        {
            ShowError("The family name did not match.");
            return RedirectToAction(nameof(Index));
        }

        Database.DeleteFamily(family.FamilyId);
        ShowSuccess("Family deleted.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public IActionResult Invite(string email)
    {
        if (!TryGetCurrentUser(out var user))
        {
            return RedirectToLogin();
        }

        var family = Database.GetFamilyByUser(user.UserId);
        if (family is null || !Database.IsFamilyAdmin(family.FamilyId, user.UserId))
        {
            ShowError("Only the family owner or an admin can invite members.");
            return RedirectToAction(nameof(Index));
        }

        email = (email ?? "").Trim().ToLowerInvariant();
        var invitedUser = Database.GetUserByEmail(email);
        if (invitedUser is null)
        {
            ShowError("The invited user must register before they can be added to the family.");
            return RedirectToAction(nameof(Index));
        }

        if (invitedUser.UserId == user.UserId)
        {
            ShowError("You are already the owner of this family.");
            return RedirectToAction(nameof(Index));
        }

        if (Database.GetFamilyByUser(invitedUser.UserId) is not null)
        {
            ShowError("Each user can belong to only one family at a time.");
            return RedirectToAction(nameof(Index));
        }

        if (Database.HasPendingFamilyInvitation(invitedUser.UserId))
        {
            ShowError("This user already has a pending family invitation.");
            return RedirectToAction(nameof(Index));
        }

        Database.AddFamilyInvitation(family.FamilyId, invitedUser.UserId, user.UserId);
        ShowSuccess("Invitation sent.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public IActionResult RemoveMember(int userId)
    {
        if (!TryGetCurrentUser(out var user))
        {
            return RedirectToLogin();
        }

        var family = Database.GetFamilyByUser(user.UserId);
        if (family is null || !Database.IsFamilyAdmin(family.FamilyId, user.UserId))
        {
            ShowError("Only the family owner or an admin can remove members.");
            return RedirectToAction(nameof(Index));
        }

        if (userId == user.UserId)
        {
            ShowError("You cannot remove yourself from the family here.");
            return RedirectToAction(nameof(Index));
        }

        Database.RemoveUserFromFamily(userId);
        ShowSuccess("Member removed.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public IActionResult AcceptInvitation(int invitationId)
    {
        if (!TryGetCurrentUser(out var user))
        {
            return RedirectToLogin();
        }

        try
        {
            Database.AcceptFamilyInvitation(invitationId, user.UserId);
            ShowSuccess("Invitation accepted.");
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public IActionResult DeclineInvitation(int invitationId)
    {
        if (!TryGetCurrentUser(out var user))
        {
            return RedirectToLogin();
        }

        Database.DeclineFamilyInvitation(invitationId, user.UserId);
        ShowSuccess("Invitation declined.");
        return RedirectToAction(nameof(Index));
    }

    private static bool IsFamilyAdmin(string? role)
    {
        return string.Equals(role, "Owner", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsFamilyOwner(string? role)
    {
        return string.Equals(role, "Owner", StringComparison.OrdinalIgnoreCase);
    }
}
