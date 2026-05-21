namespace SharedData;

public class Expense
{
    public int ExpenseId { get; set; }
    public int UserId { get; set; }
    public int CategoryId { get; set; }
    public string Description { get; set; } = "";
    public double Amount { get; set; }
    public string Date { get; set; } = "";
}

public enum ExpenseSortOption
{
    NewestFirst,
    OldestFirst,
    HighestAmount,
    LowestAmount
}

public sealed class ExpenseQueryOptions
{
    public int? CategoryId { get; init; }
    public ExpenseSortOption SortOption { get; init; } = ExpenseSortOption.NewestFirst;
}
