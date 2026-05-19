using System.Collections.ObjectModel;
using SharedData;

namespace MAUIdesktop;

public partial class FamilyPage : ContentPage
{
    private readonly DatabaseRepository _db;
    private readonly ObservableCollection<FamilyMemberViewModel> _members = new();
    private readonly ObservableCollection<FamilyInvitationViewModel> _invitations = new();
    private readonly ObservableCollection<FamilyExpenseRowViewModel> _familyExpenseRows = new();
    private readonly ObservableCollection<FamilyBudgetSummaryViewModel> _familyBudgetSummaryRows = new();
    private readonly List<FamilyMemberFilterOptionViewModel> _familyMemberFilterOptions = new();
    private readonly List<FamilyCategoryFilterOptionViewModel> _familyCategoryFilterOptions = new();
    private Family? _currentFamily;
    private bool _currentUserCanManageMembers;

    public ObservableCollection<FamilyMemberViewModel> FamilyMembers => _members;
    public ObservableCollection<FamilyInvitationViewModel> PendingInvitations => _invitations;
    public ObservableCollection<FamilyExpenseRowViewModel> FamilyExpenseRows => _familyExpenseRows;
    public ObservableCollection<FamilyBudgetSummaryViewModel> FamilyBudgetSummaryRows => _familyBudgetSummaryRows;

    public FamilyPage()
    {
        InitializeComponent();
        _db = AppDatabase.Instance;
        BindingContext = this;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (AppSession.CurrentUser is null)
        {
            Shell.Current.GoToAsync("//LoginPage");
            return;
        }

        LoadFamily();
    }

    private async void OnPersonalClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//MainPage");
    }

    private async void OnCreateFamilyClicked(object sender, EventArgs e)
    {
        var user = AppSession.CurrentUser;
        if (user is null)
        {
            return;
        }

        if (_db.GetFamilyByUser(user.UserId) is not null)
        {
            await DisplayAlert("Family already exists", "You already belong to a family.", "OK");
            LoadFamily();
            return;
        }

        var familyName = await DisplayPromptAsync("Create family", "Family name", "Create", "Cancel", $"{user.Name}'s family");
        familyName = (familyName ?? "").Trim();

        if (string.IsNullOrWhiteSpace(familyName))
        {
            return;
        }

        try
        {
            _db.CreateFamily(user.UserId, familyName, "Owner");
            LoadFamily();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Family not created", ex.Message, "OK");
        }
    }

    private async void OnInviteMemberClicked(object sender, EventArgs e)
    {
        if (_currentFamily is null || !_currentUserCanManageMembers)
        {
            await DisplayAlert("Permission required", "Only the family owner or an admin can invite members.", "OK");
            return;
        }

        var email = await DisplayPromptAsync("Invite member", "Registered user's email", "Invite", "Cancel", "user@example.com");
        email = (email ?? "").Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(email))
        {
            return;
        }

        var invitedUser = _db.GetUserByEmail(email);
        if (invitedUser is null)
        {
            await DisplayAlert("User not found", "The invited user must register before they can be added to the family.", "OK");
            return;
        }

        if (AppSession.CurrentUser?.UserId == invitedUser.UserId)
        {
            await DisplayAlert("Already a member", "You are already the owner of this family.", "OK");
            return;
        }

        if (_db.GetFamilyByUser(invitedUser.UserId) is not null)
        {
            await DisplayAlert("User already has a family", "Each user can belong to only one family at a time.", "OK");
            return;
        }

        if (_db.HasPendingFamilyInvitation(invitedUser.UserId))
        {
            await DisplayAlert("Invitation already pending", "This user already has a pending family invitation.", "OK");
            return;
        }

        try
        {
            _db.AddFamilyInvitation(_currentFamily.FamilyId, invitedUser.UserId, AppSession.CurrentUser!.UserId);
            await DisplayAlert("Invitation sent", $"{invitedUser.Name} can accept the invitation from the Family page.", "OK");
            LoadFamily();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Invitation not sent", ex.Message, "OK");
        }
    }

    private async void OnRemoveMemberClicked(object sender, EventArgs e)
    {
        if (!_currentUserCanManageMembers || (sender as Button)?.CommandParameter is not FamilyMemberViewModel member)
        {
            return;
        }

        var confirmed = await DisplayAlert(
            "Remove member",
            $"Remove {member.Name} from the family?",
            "Remove",
            "Cancel");

        if (!confirmed)
        {
            return;
        }

        try
        {
            _db.RemoveUserFromFamily(member.UserId);
            LoadFamily();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Member not removed", ex.Message, "OK");
        }
    }

    private async void OnAcceptInvitationClicked(object sender, EventArgs e)
    {
        var user = AppSession.CurrentUser;
        if (user is null || (sender as Button)?.CommandParameter is not FamilyInvitationViewModel invitation)
        {
            return;
        }

        try
        {
            _db.AcceptFamilyInvitation(invitation.InvitationId, user.UserId);
            LoadFamily();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Invitation not accepted", ex.Message, "OK");
        }
    }

    private void OnDeclineInvitationClicked(object sender, EventArgs e)
    {
        var user = AppSession.CurrentUser;
        if (user is null || (sender as Button)?.CommandParameter is not FamilyInvitationViewModel invitation)
        {
            return;
        }

        _db.DeclineFamilyInvitation(invitation.InvitationId, user.UserId);
        LoadFamily();
    }

    private void LoadFamily()
    {
        var user = AppSession.CurrentUser;
        _members.Clear();
        _invitations.Clear();
        _familyExpenseRows.Clear();
        _familyBudgetSummaryRows.Clear();

        if (user is null)
        {
            return;
        }

        _currentFamily = _db.GetFamilyByUser(user.UserId);

        if (_currentFamily is null)
        {
            _currentUserCanManageMembers = false;
            FamilyStatusLabel.Text = "Create a family to invite members and manage shared access.";
            NoFamilyPanel.IsVisible = true;
            FamilyWorkspace.IsVisible = false;

            foreach (var invitation in _db.GetPendingFamilyInvitations(user.UserId))
            {
                _invitations.Add(new FamilyInvitationViewModel(invitation));
            }

            NoInvitationsLabel.IsVisible = _invitations.Count == 0;
            NoMembersLabel.IsVisible = false;
            return;
        }

        var familyUsers = _db.GetFamilyUsers(_currentFamily.FamilyId);
        var currentMember = familyUsers.FirstOrDefault(member => member.UserId == user.UserId);

        _currentUserCanManageMembers = IsFamilyAdmin(currentMember?.Role);
        FamilyStatusLabel.Text = "Manage your family members and shared access.";
        FamilyNameLabel.Text = _currentFamily.Name;
        FamilyRoleLabel.Text = $"Your role: {currentMember?.Role ?? "Member"}";
        InviteButton.IsVisible = _currentUserCanManageMembers;
        NoFamilyPanel.IsVisible = false;
        FamilyWorkspace.IsVisible = true;
        FamilyBudgetOverviewPanel.IsVisible = _currentUserCanManageMembers;

        foreach (var member in familyUsers)
        {
            _members.Add(new FamilyMemberViewModel(
                member.UserId,
                member.Name,
                member.Email,
                member.Role,
                _currentUserCanManageMembers && member.UserId != user.UserId));
        }

        NoInvitationsLabel.IsVisible = false;
        NoMembersLabel.IsVisible = _members.Count == 0;
        LoadFamilyExpenseFilterOptions(familyUsers);
        RefreshFamilyExpenseRows();
        RefreshFamilyBudgetOverview();
    }

    private static bool IsFamilyAdmin(string? role)
    {
        return string.Equals(role, "Owner", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase);
    }

    private void LoadFamilyExpenseFilterOptions(IReadOnlyCollection<FamilyUser> familyUsers)
    {
        if (_currentFamily is null)
        {
            return;
        }

        var selectedUserId = (FamilyMemberFilterPicker.SelectedItem as FamilyMemberFilterOptionViewModel)?.UserId;
        var selectedCategoryName = (FamilyCategoryFilterPicker.SelectedItem as FamilyCategoryFilterOptionViewModel)?.CategoryName;

        _familyMemberFilterOptions.Clear();
        _familyMemberFilterOptions.Add(new FamilyMemberFilterOptionViewModel(null, "All members"));
        _familyMemberFilterOptions.AddRange(familyUsers.Select(member =>
            new FamilyMemberFilterOptionViewModel(member.UserId, member.Name)));

        _familyCategoryFilterOptions.Clear();
        _familyCategoryFilterOptions.Add(new FamilyCategoryFilterOptionViewModel(null, "All categories"));
        _familyCategoryFilterOptions.AddRange(_db.GetFamilyCategoryNames(_currentFamily.FamilyId)
            .Select(categoryName => new FamilyCategoryFilterOptionViewModel(categoryName, categoryName)));

        FamilyMemberFilterPicker.ItemsSource = null;
        FamilyMemberFilterPicker.ItemsSource = _familyMemberFilterOptions;
        FamilyMemberFilterPicker.SelectedItem = _familyMemberFilterOptions
            .FirstOrDefault(option => option.UserId == selectedUserId)
            ?? _familyMemberFilterOptions[0];

        FamilyCategoryFilterPicker.ItemsSource = null;
        FamilyCategoryFilterPicker.ItemsSource = _familyCategoryFilterOptions;
        FamilyCategoryFilterPicker.SelectedItem = _familyCategoryFilterOptions
            .FirstOrDefault(option => option.CategoryName == selectedCategoryName)
            ?? _familyCategoryFilterOptions[0];
    }

    private async void OnApplyFamilyExpenseFiltersClicked(object sender, EventArgs e)
    {
        if (!TryReadDate(FamilyExpenseStartDateEntry.Text, out _) ||
            !TryReadDate(FamilyExpenseEndDateEntry.Text, out _))
        {
            await DisplayAlert("Invalid date", "Use yyyy-MM-dd for date filters, or leave the field blank.", "OK");
            return;
        }

        RefreshFamilyExpenseRows();
    }

    private void RefreshFamilyExpenseRows()
    {
        _familyExpenseRows.Clear();

        if (_currentFamily is null)
        {
            FamilyExpensesCaptionLabel.Text = "";
            return;
        }

        TryReadDate(FamilyExpenseStartDateEntry.Text, out var startDate);
        TryReadDate(FamilyExpenseEndDateEntry.Text, out var endDate);

        var selectedMember = FamilyMemberFilterPicker.SelectedItem as FamilyMemberFilterOptionViewModel;
        var selectedCategory = FamilyCategoryFilterPicker.SelectedItem as FamilyCategoryFilterOptionViewModel;
        var query = new FamilyExpenseQuery
        {
            MemberUserId = selectedMember?.UserId,
            CategoryName = selectedCategory?.CategoryName,
            StartDate = startDate,
            EndDate = endDate
        };

        var rows = _db.GetFamilyExpenseRows(_currentFamily.FamilyId, query);
        foreach (var row in rows.Select(row => new FamilyExpenseRowViewModel(row)))
        {
            _familyExpenseRows.Add(row);
        }

        var total = rows.Sum(row => row.Amount);
        FamilyExpensesCaptionLabel.Text = rows.Count == 0
            ? "No family expenses match the selected filters."
            : $"{rows.Count} expense{(rows.Count == 1 ? "" : "s")} · total {total:C}";
    }

    private void RefreshFamilyBudgetOverview()
    {
        _familyBudgetSummaryRows.Clear();

        if (_currentFamily is null || !_currentUserCanManageMembers)
        {
            FamilyBudgetOverviewCaptionLabel.Text = "Only family owners and admins can view combined family totals.";
            FamilyBudgetOverviewTotalLabel.Text = "";
            FamilyBudgetOverviewPanel.IsVisible = _currentFamily is not null && _currentUserCanManageMembers;
            return;
        }

        FamilyBudgetOverviewPanel.IsVisible = true;
        var summaries = _db.GetFamilyBudgetOverview(_currentFamily.FamilyId);
        foreach (var summary in summaries.Select(summary => new FamilyBudgetSummaryViewModel(summary)))
        {
            _familyBudgetSummaryRows.Add(summary);
        }

        var total = summaries.Sum(summary => summary.TotalSpent);
        FamilyBudgetOverviewTotalLabel.Text = total.ToString("C");
        FamilyBudgetOverviewCaptionLabel.Text = summaries.Count == 0
            ? "No family spending has been recorded yet."
            : $"{summaries.Count} categor{(summaries.Count == 1 ? "y" : "ies")} across all family members.";
    }

    private static bool TryReadDate(string? value, out DateTime? date)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            date = null;
            return true;
        }

        if (DateTime.TryParseExact(
                value.Trim(),
                "yyyy-MM-dd",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None,
                out var parsedDate))
        {
            date = parsedDate;
            return true;
        }

        date = null;
        return false;
    }
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
