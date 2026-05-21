namespace SharedData;

public interface IDatabaseRepository
{
    int AddUser(string email, string name, string password);
    User? GetUser(int userId);
    User? GetUserByEmail(string email);
    void UpdateUser(int userId, string name, string password);
    void DeleteUser(int userId);

    int AddExpense(int userId, int categoryId, string description, double amount, string date);
    Expense? GetExpense(int expenseId, int userId);
    List<Expense> GetAllExpensesByUser(int userId);
    void UpdateExpense(int expenseId, int userId, int categoryId, string description, double amount, string date);
    void DeleteExpense(int expenseId, int userId);

    int AddCategory(int userId, string name, double remainingBudget, double? warningThreshold = null);
    Category? GetCategory(int categoryId, int userId);
    List<Category> GetAllCategoriesByUser(int userId);
    void UpdateCategory(int categoryId, int userId, string name, double remainingBudget, double? warningThreshold = null);
    void DeleteCategory(int categoryId, int userId);

    int AddGoal(int userId, string name, double targetAmount, DateTime deadline, DateTime? createdAt = null);
    void UpdateGoal(int goalId, int userId, string name, double targetAmount, DateTime deadline);
    void DeleteGoal(int goalId, int userId);
    List<Goal> GetGoalsByUser(int userId);
    List<GoalEvent> EvaluateGoalsForUser(int userId, DateTime? asOf = null);
    List<GoalEvent> GetRecentGoalEvents(int userId, int limit = 5);

    int CreateFamily(int ownerUserId, string familyName, string role = "Owner");
    string? GetFamilyName(int familyId);
    Family? GetFamily(int familyId);
    Family? GetFamilyByUser(int userId);
    void UpdateFamily(int familyId, string name);
    void DeleteFamily(int familyId);
    List<FamilyUser> GetFamilyUsers(int familyId);
    string? GetFamilyMemberRole(int familyId, int userId);
    bool IsFamilyAdmin(int familyId, int userId);
    List<string> GetFamilyCategoryNames(int familyId);
    List<FamilyExpenseRow> GetFamilyExpenseRows(int familyId, FamilyExpenseQuery? query = null);
    List<FamilyBudgetCategorySummary> GetFamilyBudgetOverview(int familyId);
    void AddUserToFamily(int userId, int familyId, string role);
    void RemoveUserFromFamily(int userId);
    void UpdateFamilyMemberRole(int userId, int familyId, string role);

    int AddFamilyInvitation(int familyId, int invitedUserId, int invitedByUserId);
    bool HasPendingFamilyInvitation(int invitedUserId);
    List<FamilyInvitation> GetPendingFamilyInvitations(int invitedUserId);
    void AcceptFamilyInvitation(int invitationId, int invitedUserId, string role = "Member");
    void DeclineFamilyInvitation(int invitationId, int invitedUserId);

    int AddFamilyCategory(int familyId, string name, double budget);
    FamilyCategory? GetFamilyCategory(int familyId, int familyCategoryId);
    List<FamilyCategory> GetAllFamilyCategories(int familyId);
    void UpdateFamilyCategory(int familyId, int familyCategoryId, string name, double budget);
    void DeleteFamilyCategory(int familyId, int familyCategoryId);
    int AddFamilyExpense(int familyId, int userId, int familyCategoryId, string description, double amount, string date);
    FamilyExpense? GetFamilyExpense(int familyId, int familyExpenseId);
    List<FamilyExpense> GetAllFamilyExpenses(int familyId);
    List<FamilyExpense> GetFamilyExpensesByUser(int familyId, int userId);
    void UpdateFamilyExpense(int familyId, int familyExpenseId, int userId, int familyCategoryId, string description, double amount, string date);
    void DeleteFamilyExpense(int familyId, int familyExpenseId);
}
