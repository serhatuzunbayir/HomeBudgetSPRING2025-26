using System.Collections.ObjectModel;
using SharedData;

namespace MAUIdesktop;

public partial class FamilyPage : ContentPage
{
    private readonly DatabaseRepository _db;
    private readonly ObservableCollection<FamilyMemberViewModel> _members = new();
    private readonly ObservableCollection<FamilyInvitationViewModel> _invitations = new();
    private Family? _currentFamily;
    private bool _currentUserCanManageMembers;

    public FamilyPage()
    {
        InitializeComponent();
        _db = AppDatabase.Instance;
        MembersCollection.ItemsSource = _members;
        InvitationsCollection.ItemsSource = _invitations;
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

        foreach (var member in familyUsers)
        {
            _members.Add(new FamilyMemberViewModel(
                member.UserId,
                member.Name,
                member.Email,
                member.Role,
                _currentUserCanManageMembers && member.UserId != user.UserId));
        }
    }

    private static bool IsFamilyAdmin(string? role)
    {
        return string.Equals(role, "Owner", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase);
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
