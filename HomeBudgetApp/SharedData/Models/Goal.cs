namespace SharedData;

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
