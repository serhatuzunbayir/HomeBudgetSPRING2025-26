namespace SharedData;

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
