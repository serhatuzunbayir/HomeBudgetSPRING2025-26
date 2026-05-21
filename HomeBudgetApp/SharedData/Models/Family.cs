namespace SharedData;

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
