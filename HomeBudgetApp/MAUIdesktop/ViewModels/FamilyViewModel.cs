using System.Collections.ObjectModel;
using SharedData;

namespace MAUIdesktop;

public sealed class FamilyViewModel : ObservableObject
{
    private readonly IDatabaseRepository _database;
    private readonly IUserDialogService _dialogs;
    private readonly IAppNavigationService _navigation;
    private Family? _currentFamily;
    private bool _currentUserCanManageMembers;
    private bool _currentUserCanDeleteFamily;
    private string _familyStatusText = "";
    private string _familyNameText = "";
    private string _familyRoleText = "";
    private bool _noFamilyPanelVisible;
    private bool _familyWorkspaceVisible;
    private bool _inviteButtonVisible;
    private bool _deleteFamilyButtonVisible;
    private bool _noInvitationsVisible;
    private bool _noMembersVisible;
    private bool _familyBudgetOverviewVisible;
    private string _familyExpensesCaption = "";
    private string _familyBudgetOverviewCaption = "";
    private string _familyBudgetOverviewTotal = "";
    private bool _useStartDate;
    private bool _useEndDate;
    private DateTime _startDate = DateTime.Today.AddMonths(-1);
    private DateTime _endDate = DateTime.Today;
    private FamilyMemberFilterOptionViewModel? _selectedMemberFilter;
    private FamilyCategoryFilterOptionViewModel? _selectedCategoryFilter;

    public FamilyViewModel(IDatabaseRepository database, IUserDialogService dialogs, IAppNavigationService navigation)
    {
        _database = database;
        _dialogs = dialogs;
        _navigation = navigation;
    }

    public ObservableCollection<FamilyMemberViewModel> FamilyMembers { get; } = new();
    public ObservableCollection<FamilyInvitationViewModel> PendingInvitations { get; } = new();
    public ObservableCollection<FamilyExpenseRowViewModel> FamilyExpenseRows { get; } = new();
    public ObservableCollection<FamilyBudgetSummaryViewModel> FamilyBudgetSummaryRows { get; } = new();
    public ObservableCollection<FamilyMemberFilterOptionViewModel> FamilyMemberFilterOptions { get; } = new();
    public ObservableCollection<FamilyCategoryFilterOptionViewModel> FamilyCategoryFilterOptions { get; } = new();

    public string FamilyStatusText { get => _familyStatusText; private set => SetProperty(ref _familyStatusText, value); }
    public string FamilyNameText { get => _familyNameText; private set => SetProperty(ref _familyNameText, value); }
    public string FamilyRoleText { get => _familyRoleText; private set => SetProperty(ref _familyRoleText, value); }
    public bool NoFamilyPanelVisible { get => _noFamilyPanelVisible; private set => SetProperty(ref _noFamilyPanelVisible, value); }
    public bool FamilyWorkspaceVisible { get => _familyWorkspaceVisible; private set => SetProperty(ref _familyWorkspaceVisible, value); }
    public bool InviteButtonVisible { get => _inviteButtonVisible; private set => SetProperty(ref _inviteButtonVisible, value); }
    public bool DeleteFamilyButtonVisible { get => _deleteFamilyButtonVisible; private set => SetProperty(ref _deleteFamilyButtonVisible, value); }
    public bool NoInvitationsVisible { get => _noInvitationsVisible; private set => SetProperty(ref _noInvitationsVisible, value); }
    public bool NoMembersVisible { get => _noMembersVisible; private set => SetProperty(ref _noMembersVisible, value); }
    public bool FamilyBudgetOverviewVisible { get => _familyBudgetOverviewVisible; private set => SetProperty(ref _familyBudgetOverviewVisible, value); }
    public string FamilyExpensesCaption { get => _familyExpensesCaption; private set => SetProperty(ref _familyExpensesCaption, value); }
    public string FamilyBudgetOverviewCaption { get => _familyBudgetOverviewCaption; private set => SetProperty(ref _familyBudgetOverviewCaption, value); }
    public string FamilyBudgetOverviewTotal { get => _familyBudgetOverviewTotal; private set => SetProperty(ref _familyBudgetOverviewTotal, value); }
    public bool StartDateEnabled => UseStartDate;
    public bool EndDateEnabled => UseEndDate;

    public bool UseStartDate
    {
        get => _useStartDate;
        set
        {
            if (SetProperty(ref _useStartDate, value))
            {
                OnPropertyChanged(nameof(StartDateEnabled));
            }
        }
    }

    public bool UseEndDate
    {
        get => _useEndDate;
        set
        {
            if (SetProperty(ref _useEndDate, value))
            {
                OnPropertyChanged(nameof(EndDateEnabled));
            }
        }
    }

    public DateTime StartDate { get => _startDate; set => SetProperty(ref _startDate, value); }
    public DateTime EndDate { get => _endDate; set => SetProperty(ref _endDate, value); }

    public FamilyMemberFilterOptionViewModel? SelectedMemberFilter
    {
        get => _selectedMemberFilter;
        set => SetProperty(ref _selectedMemberFilter, value);
    }

    public FamilyCategoryFilterOptionViewModel? SelectedCategoryFilter
    {
        get => _selectedCategoryFilter;
        set => SetProperty(ref _selectedCategoryFilter, value);
    }

    public bool IsSignedIn => AppSession.CurrentUser is not null;

    public Task OpenPersonalAsync() => _navigation.GoToAsync("//MainPage");

    public async Task CreateFamilyAsync()
    {
        var user = AppSession.CurrentUser;
        if (user is null)
        {
            return;
        }

        if (_database.GetFamilyByUser(user.UserId) is not null)
        {
            await _dialogs.AlertAsync("Family already exists", "You already belong to a family.");
            LoadFamily();
            return;
        }

        var familyName = (await _dialogs.PromptAsync("Create family", "Family name", "Create", "Cancel", $"{user.Name}'s family") ?? "").Trim();
        if (string.IsNullOrWhiteSpace(familyName))
        {
            return;
        }

        try
        {
            _database.CreateFamily(user.UserId, familyName, "Owner");
            LoadFamily();
        }
        catch (Exception ex)
        {
            await _dialogs.AlertAsync("Family not created", ex.Message);
        }
    }

    public async Task InviteMemberAsync()
    {
        if (_currentFamily is null || !_currentUserCanManageMembers)
        {
            await _dialogs.AlertAsync("Permission required", "Only the family owner or an admin can invite members.");
            return;
        }

        var email = (await _dialogs.PromptAsync("Invite member", "Registered user's email", "Invite", "Cancel", "user@example.com") ?? "").Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(email))
        {
            return;
        }

        var invitedUser = _database.GetUserByEmail(email);
        if (invitedUser is null)
        {
            await _dialogs.AlertAsync("User not found", "The invited user must register before they can be added to the family.");
            return;
        }

        if (AppSession.CurrentUser?.UserId == invitedUser.UserId)
        {
            await _dialogs.AlertAsync("Already a member", "You are already the owner of this family.");
            return;
        }

        if (_database.GetFamilyByUser(invitedUser.UserId) is not null)
        {
            await _dialogs.AlertAsync("User already has a family", "Each user can belong to only one family at a time.");
            return;
        }

        if (_database.HasPendingFamilyInvitation(invitedUser.UserId))
        {
            await _dialogs.AlertAsync("Invitation already pending", "This user already has a pending family invitation.");
            return;
        }

        try
        {
            _database.AddFamilyInvitation(_currentFamily.FamilyId, invitedUser.UserId, AppSession.CurrentUser!.UserId);
            await _dialogs.AlertAsync("Invitation sent", $"{invitedUser.Name} can accept the invitation from the Family page.");
            LoadFamily();
        }
        catch (Exception ex)
        {
            await _dialogs.AlertAsync("Invitation not sent", ex.Message);
        }
    }

    public async Task DeleteFamilyAsync()
    {
        if (_currentFamily is null || !_currentUserCanDeleteFamily)
        {
            await _dialogs.AlertAsync("Permission required", "Only the family owner can delete the family workspace.");
            return;
        }

        var familyName = _currentFamily.Name;
        var confirmed = await _dialogs.ConfirmAsync(
            "Delete family",
            $"Delete {familyName}? This removes the family workspace, memberships, and pending invitations. Personal categories and expenses stay in each account.",
            "Continue",
            "Cancel");

        if (!confirmed)
        {
            return;
        }

        var typedName = await _dialogs.PromptAsync(
            "Confirm delete",
            $"Type \"{familyName}\" to delete this family.",
            "Delete",
            "Cancel",
            familyName);

        if (!string.Equals((typedName ?? "").Trim(), familyName, StringComparison.Ordinal))
        {
            await _dialogs.AlertAsync("Delete cancelled", "The family name did not match.");
            return;
        }

        try
        {
            _database.DeleteFamily(_currentFamily.FamilyId);
            await _dialogs.AlertAsync("Family deleted", $"{familyName} was deleted.");
            LoadFamily();
        }
        catch (Exception ex)
        {
            await _dialogs.AlertAsync("Family not deleted", ex.Message);
        }
    }

    public async Task RemoveMemberAsync(FamilyMemberViewModel member)
    {
        if (!_currentUserCanManageMembers)
        {
            return;
        }

        var confirmed = await _dialogs.ConfirmAsync("Remove member", $"Remove {member.Name} from the family?", "Remove", "Cancel");
        if (!confirmed)
        {
            return;
        }

        try
        {
            _database.RemoveUserFromFamily(member.UserId);
            LoadFamily();
        }
        catch (Exception ex)
        {
            await _dialogs.AlertAsync("Member not removed", ex.Message);
        }
    }

    public async Task AcceptInvitationAsync(FamilyInvitationViewModel invitation)
    {
        var user = AppSession.CurrentUser;
        if (user is null)
        {
            return;
        }

        try
        {
            _database.AcceptFamilyInvitation(invitation.InvitationId, user.UserId);
            LoadFamily();
        }
        catch (Exception ex)
        {
            await _dialogs.AlertAsync("Invitation not accepted", ex.Message);
        }
    }

    public void DeclineInvitation(FamilyInvitationViewModel invitation)
    {
        var user = AppSession.CurrentUser;
        if (user is null)
        {
            return;
        }

        _database.DeclineFamilyInvitation(invitation.InvitationId, user.UserId);
        LoadFamily();
    }

    // Loads the current family workspace, members, invitations, and budget summary.
    public void LoadFamily()
    {
        var user = AppSession.CurrentUser;
        FamilyMembers.Clear();
        PendingInvitations.Clear();
        FamilyExpenseRows.Clear();
        FamilyBudgetSummaryRows.Clear();

        if (user is null)
        {
            return;
        }

        _currentFamily = _database.GetFamilyByUser(user.UserId);

        if (_currentFamily is null)
        {
            _currentUserCanManageMembers = false;
            _currentUserCanDeleteFamily = false;
            FamilyStatusText = "Create a family to invite members and manage shared access.";
            NoFamilyPanelVisible = true;
            FamilyWorkspaceVisible = false;
            FamilyBudgetOverviewVisible = false;

            foreach (var invitation in _database.GetPendingFamilyInvitations(user.UserId))
            {
                PendingInvitations.Add(new FamilyInvitationViewModel(invitation));
            }

            NoInvitationsVisible = PendingInvitations.Count == 0;
            NoMembersVisible = false;
            return;
        }

        var familyUsers = _database.GetFamilyUsers(_currentFamily.FamilyId);
        var currentMember = familyUsers.FirstOrDefault(member => member.UserId == user.UserId);

        _currentUserCanManageMembers = IsFamilyAdmin(currentMember?.Role);
        _currentUserCanDeleteFamily = IsFamilyOwner(currentMember?.Role);
        FamilyStatusText = "Manage your family members and shared access.";
        FamilyNameText = _currentFamily.Name;
        FamilyRoleText = $"Your role: {currentMember?.Role ?? "Member"}";
        InviteButtonVisible = _currentUserCanManageMembers;
        DeleteFamilyButtonVisible = _currentUserCanDeleteFamily;
        NoFamilyPanelVisible = false;
        FamilyWorkspaceVisible = true;
        FamilyBudgetOverviewVisible = _currentUserCanManageMembers;

        foreach (var member in familyUsers)
        {
            FamilyMembers.Add(new FamilyMemberViewModel(
                member.UserId,
                member.Name,
                member.Email,
                member.Role,
                _currentUserCanManageMembers && member.UserId != user.UserId));
        }

        NoInvitationsVisible = false;
        NoMembersVisible = FamilyMembers.Count == 0;
        LoadFamilyExpenseFilterOptions(familyUsers);
        RefreshFamilyExpenseRows();
        RefreshFamilyBudgetOverview();
    }

    // Applies the selected member, category, and date filters to family expenses.
    public void RefreshFamilyExpenseRows()
    {
        FamilyExpenseRows.Clear();

        if (_currentFamily is null)
        {
            FamilyExpensesCaption = "";
            return;
        }

        if (!TryGetFamilyDateRange(out var startDate, out var endDate))
        {
            FamilyExpensesCaption = "Start date must be on or before end date.";
            return;
        }

        var query = new FamilyExpenseQuery
        {
            MemberUserId = SelectedMemberFilter?.UserId,
            CategoryName = SelectedCategoryFilter?.CategoryName,
            StartDate = startDate,
            EndDate = endDate
        };

        var rows = _database.GetFamilyExpenseRows(_currentFamily.FamilyId, query);
        foreach (var row in rows.Select(row => new FamilyExpenseRowViewModel(row)))
        {
            FamilyExpenseRows.Add(row);
        }

        var total = rows.Sum(row => row.Amount);
        FamilyExpensesCaption = rows.Count == 0
            ? "No family expenses match the selected filters."
            : $"{rows.Count} expense{(rows.Count == 1 ? "" : "s")} · total {total:C}";
    }

    public async Task ApplyFamilyExpenseFiltersAsync()
    {
        if (!TryGetFamilyDateRange(out _, out _))
        {
            await _dialogs.AlertAsync("Invalid date range", "Start date must be on or before end date.");
            return;
        }

        RefreshFamilyExpenseRows();
    }

    public void ClearFamilyExpenseFilters()
    {
        if (FamilyMemberFilterOptions.Count > 0)
        {
            SelectedMemberFilter = FamilyMemberFilterOptions[0];
        }

        if (FamilyCategoryFilterOptions.Count > 0)
        {
            SelectedCategoryFilter = FamilyCategoryFilterOptions[0];
        }

        UseStartDate = false;
        UseEndDate = false;
        StartDate = DateTime.Today.AddMonths(-1);
        EndDate = DateTime.Today;
        RefreshFamilyExpenseRows();
    }

    private void LoadFamilyExpenseFilterOptions(IReadOnlyCollection<FamilyUser> familyUsers)
    {
        if (_currentFamily is null)
        {
            return;
        }

        var selectedUserId = SelectedMemberFilter?.UserId;
        var selectedCategoryName = SelectedCategoryFilter?.CategoryName;

        FamilyMemberFilterOptions.Clear();
        FamilyMemberFilterOptions.Add(new FamilyMemberFilterOptionViewModel(null, "All members"));
        foreach (var member in familyUsers)
        {
            FamilyMemberFilterOptions.Add(new FamilyMemberFilterOptionViewModel(member.UserId, member.Name));
        }

        FamilyCategoryFilterOptions.Clear();
        FamilyCategoryFilterOptions.Add(new FamilyCategoryFilterOptionViewModel(null, "All categories"));
        foreach (var categoryName in _database.GetFamilyCategoryNames(_currentFamily.FamilyId))
        {
            FamilyCategoryFilterOptions.Add(new FamilyCategoryFilterOptionViewModel(categoryName, categoryName));
        }

        SelectedMemberFilter = FamilyMemberFilterOptions
            .FirstOrDefault(option => option.UserId == selectedUserId)
            ?? FamilyMemberFilterOptions[0];

        SelectedCategoryFilter = FamilyCategoryFilterOptions
            .FirstOrDefault(option => option.CategoryName == selectedCategoryName)
            ?? FamilyCategoryFilterOptions[0];
    }

    private void RefreshFamilyBudgetOverview()
    {
        FamilyBudgetSummaryRows.Clear();

        if (_currentFamily is null || !_currentUserCanManageMembers)
        {
            FamilyBudgetOverviewCaption = "Only family owners and admins can view combined family totals.";
            FamilyBudgetOverviewTotal = "";
            FamilyBudgetOverviewVisible = _currentFamily is not null && _currentUserCanManageMembers;
            return;
        }

        FamilyBudgetOverviewVisible = true;
        var summaries = _database.GetFamilyBudgetOverview(_currentFamily.FamilyId);
        foreach (var summary in summaries.Select(summary => new FamilyBudgetSummaryViewModel(summary)))
        {
            FamilyBudgetSummaryRows.Add(summary);
        }

        FamilyBudgetOverviewTotal = summaries.Sum(summary => summary.TotalSpent).ToString("C");
        FamilyBudgetOverviewCaption = summaries.Count == 0
            ? "No family spending has been recorded yet."
            : $"{summaries.Count} categor{(summaries.Count == 1 ? "y" : "ies")} across all family members.";
    }

    private bool TryGetFamilyDateRange(out DateTime? startDate, out DateTime? endDate)
    {
        startDate = UseStartDate ? StartDate : null;
        endDate = UseEndDate ? EndDate : null;

        if (startDate.HasValue && endDate.HasValue && startDate.Value.Date > endDate.Value.Date)
        {
            return false;
        }

        return true;
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
