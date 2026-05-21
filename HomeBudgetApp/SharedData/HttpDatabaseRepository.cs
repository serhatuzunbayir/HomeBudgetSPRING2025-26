using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SharedData;

public sealed class HttpDatabaseRepository : IDatabaseRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _httpClient;

    public HttpDatabaseRepository(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public int AddUser(string email, string name, string password)
    {
        return Invoke<int>(nameof(AddUser), email, name, password);
    }

    public User? GetUser(int userId)
    {
        return Invoke<User?>(nameof(GetUser), userId);
    }

    public User? GetUserByEmail(string email)
    {
        return Invoke<User?>(nameof(GetUserByEmail), email);
    }

    public void UpdateUser(int userId, string name, string password)
    {
        Invoke(nameof(UpdateUser), userId, name, password);
    }

    public void DeleteUser(int userId)
    {
        Invoke(nameof(DeleteUser), userId);
    }

    public int AddExpense(int userId, int categoryId, string description, double amount, string date)
    {
        return Invoke<int>(nameof(AddExpense), userId, categoryId, description, amount, date);
    }

    public Expense? GetExpense(int expenseId, int userId)
    {
        return Invoke<Expense?>(nameof(GetExpense), expenseId, userId);
    }

    public List<Expense> GetAllExpensesByUser(int userId)
    {
        return Invoke<List<Expense>>(nameof(GetAllExpensesByUser), userId);
    }

    public void UpdateExpense(int expenseId, int userId, int categoryId, string description, double amount, string date)
    {
        Invoke(nameof(UpdateExpense), expenseId, userId, categoryId, description, amount, date);
    }

    public void DeleteExpense(int expenseId, int userId)
    {
        Invoke(nameof(DeleteExpense), expenseId, userId);
    }

    public int AddCategory(int userId, string name, double remainingBudget, double? warningThreshold = null)
    {
        return Invoke<int>(nameof(AddCategory), userId, name, remainingBudget, warningThreshold);
    }

    public Category? GetCategory(int categoryId, int userId)
    {
        return Invoke<Category?>(nameof(GetCategory), categoryId, userId);
    }

    public List<Category> GetAllCategoriesByUser(int userId)
    {
        return Invoke<List<Category>>(nameof(GetAllCategoriesByUser), userId);
    }

    public void UpdateCategory(int categoryId, int userId, string name, double remainingBudget, double? warningThreshold = null)
    {
        Invoke(nameof(UpdateCategory), categoryId, userId, name, remainingBudget, warningThreshold);
    }

    public void DeleteCategory(int categoryId, int userId)
    {
        Invoke(nameof(DeleteCategory), categoryId, userId);
    }

    public int AddGoal(int userId, string name, double targetAmount, DateTime deadline, DateTime? createdAt = null)
    {
        return Invoke<int>(nameof(AddGoal), userId, name, targetAmount, deadline, createdAt);
    }

    public void UpdateGoal(int goalId, int userId, string name, double targetAmount, DateTime deadline)
    {
        Invoke(nameof(UpdateGoal), goalId, userId, name, targetAmount, deadline);
    }

    public void DeleteGoal(int goalId, int userId)
    {
        Invoke(nameof(DeleteGoal), goalId, userId);
    }

    public List<Goal> GetGoalsByUser(int userId)
    {
        return Invoke<List<Goal>>(nameof(GetGoalsByUser), userId);
    }

    public List<GoalEvent> EvaluateGoalsForUser(int userId, DateTime? asOf = null)
    {
        return Invoke<List<GoalEvent>>(nameof(EvaluateGoalsForUser), userId, asOf);
    }

    public List<GoalEvent> GetRecentGoalEvents(int userId, int limit = 5)
    {
        return Invoke<List<GoalEvent>>(nameof(GetRecentGoalEvents), userId, limit);
    }

    public int CreateFamily(int ownerUserId, string familyName, string role = "Owner")
    {
        return Invoke<int>(nameof(CreateFamily), ownerUserId, familyName, role);
    }

    public string? GetFamilyName(int familyId)
    {
        return Invoke<string?>(nameof(GetFamilyName), familyId);
    }

    public Family? GetFamily(int familyId)
    {
        return Invoke<Family?>(nameof(GetFamily), familyId);
    }

    public Family? GetFamilyByUser(int userId)
    {
        return Invoke<Family?>(nameof(GetFamilyByUser), userId);
    }

    public void UpdateFamily(int familyId, string name)
    {
        Invoke(nameof(UpdateFamily), familyId, name);
    }

    public void DeleteFamily(int familyId)
    {
        Invoke(nameof(DeleteFamily), familyId);
    }

    public List<FamilyUser> GetFamilyUsers(int familyId)
    {
        return Invoke<List<FamilyUser>>(nameof(GetFamilyUsers), familyId);
    }

    public string? GetFamilyMemberRole(int familyId, int userId)
    {
        return Invoke<string?>(nameof(GetFamilyMemberRole), familyId, userId);
    }

    public bool IsFamilyAdmin(int familyId, int userId)
    {
        return Invoke<bool>(nameof(IsFamilyAdmin), familyId, userId);
    }

    public List<string> GetFamilyCategoryNames(int familyId)
    {
        return Invoke<List<string>>(nameof(GetFamilyCategoryNames), familyId);
    }

    public List<FamilyExpenseRow> GetFamilyExpenseRows(int familyId, FamilyExpenseQuery? query = null)
    {
        return Invoke<List<FamilyExpenseRow>>(nameof(GetFamilyExpenseRows), familyId, query);
    }

    public List<FamilyBudgetCategorySummary> GetFamilyBudgetOverview(int familyId)
    {
        return Invoke<List<FamilyBudgetCategorySummary>>(nameof(GetFamilyBudgetOverview), familyId);
    }

    public void AddUserToFamily(int userId, int familyId, string role)
    {
        Invoke(nameof(AddUserToFamily), userId, familyId, role);
    }

    public void RemoveUserFromFamily(int userId)
    {
        Invoke(nameof(RemoveUserFromFamily), userId);
    }

    public void UpdateFamilyMemberRole(int userId, int familyId, string role)
    {
        Invoke(nameof(UpdateFamilyMemberRole), userId, familyId, role);
    }

    public int AddFamilyInvitation(int familyId, int invitedUserId, int invitedByUserId)
    {
        return Invoke<int>(nameof(AddFamilyInvitation), familyId, invitedUserId, invitedByUserId);
    }

    public bool HasPendingFamilyInvitation(int invitedUserId)
    {
        return Invoke<bool>(nameof(HasPendingFamilyInvitation), invitedUserId);
    }

    public List<FamilyInvitation> GetPendingFamilyInvitations(int invitedUserId)
    {
        return Invoke<List<FamilyInvitation>>(nameof(GetPendingFamilyInvitations), invitedUserId);
    }

    public void AcceptFamilyInvitation(int invitationId, int invitedUserId, string role = "Member")
    {
        Invoke(nameof(AcceptFamilyInvitation), invitationId, invitedUserId, role);
    }

    public void DeclineFamilyInvitation(int invitationId, int invitedUserId)
    {
        Invoke(nameof(DeclineFamilyInvitation), invitationId, invitedUserId);
    }

    public int AddFamilyCategory(int familyId, string name, double budget)
    {
        return Invoke<int>(nameof(AddFamilyCategory), familyId, name, budget);
    }

    public FamilyCategory? GetFamilyCategory(int familyId, int familyCategoryId)
    {
        return Invoke<FamilyCategory?>(nameof(GetFamilyCategory), familyId, familyCategoryId);
    }

    public List<FamilyCategory> GetAllFamilyCategories(int familyId)
    {
        return Invoke<List<FamilyCategory>>(nameof(GetAllFamilyCategories), familyId);
    }

    public void UpdateFamilyCategory(int familyId, int familyCategoryId, string name, double budget)
    {
        Invoke(nameof(UpdateFamilyCategory), familyId, familyCategoryId, name, budget);
    }

    public void DeleteFamilyCategory(int familyId, int familyCategoryId)
    {
        Invoke(nameof(DeleteFamilyCategory), familyId, familyCategoryId);
    }

    public int AddFamilyExpense(int familyId, int userId, int familyCategoryId, string description, double amount, string date)
    {
        return Invoke<int>(nameof(AddFamilyExpense), familyId, userId, familyCategoryId, description, amount, date);
    }

    public FamilyExpense? GetFamilyExpense(int familyId, int familyExpenseId)
    {
        return Invoke<FamilyExpense?>(nameof(GetFamilyExpense), familyId, familyExpenseId);
    }

    public List<FamilyExpense> GetAllFamilyExpenses(int familyId)
    {
        return Invoke<List<FamilyExpense>>(nameof(GetAllFamilyExpenses), familyId);
    }

    public List<FamilyExpense> GetFamilyExpensesByUser(int familyId, int userId)
    {
        return Invoke<List<FamilyExpense>>(nameof(GetFamilyExpensesByUser), familyId, userId);
    }

    public void UpdateFamilyExpense(int familyId, int familyExpenseId, int userId, int familyCategoryId, string description, double amount, string date)
    {
        Invoke(nameof(UpdateFamilyExpense), familyId, familyExpenseId, userId, familyCategoryId, description, amount, date);
    }

    public void DeleteFamilyExpense(int familyId, int familyExpenseId)
    {
        Invoke(nameof(DeleteFamilyExpense), familyId, familyExpenseId);
    }

    private T Invoke<T>(string methodName, params object?[] arguments)
    {
        using var response = Send(methodName, arguments);
        var result = response.Content.ReadFromJsonAsync<T>(JsonOptions).GetAwaiter().GetResult();
        return result!;
    }

    private void Invoke(string methodName, params object?[] arguments)
    {
        using var response = Send(methodName, arguments);
    }

    private HttpResponseMessage Send(string methodName, object?[] arguments)
    {
        var request = new RepositoryCallRequest
        {
            Arguments = arguments
                .Select(argument => JsonSerializer.SerializeToElement(argument, argument?.GetType() ?? typeof(object), JsonOptions))
                .ToArray()
        };

        var response = _httpClient.PostAsJsonAsync($"api/database/{methodName}", request, JsonOptions)
            .GetAwaiter()
            .GetResult();

        if (response.IsSuccessStatusCode)
        {
            return response;
        }

        var errorMessage = ReadErrorMessage(response);
        response.Dispose();
        throw new InvalidOperationException(errorMessage);
    }

    private static string ReadErrorMessage(HttpResponseMessage response)
    {
        try
        {
            var error = response.Content.ReadFromJsonAsync<RepositoryErrorResponse>(JsonOptions)
                .GetAwaiter()
                .GetResult();
            if (!string.IsNullOrWhiteSpace(error?.Message))
            {
                return error.Message;
            }
        }
        catch
        {
            // Fall back to the raw response body below.
        }

        var fallback = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        return string.IsNullOrWhiteSpace(fallback)
            ? $"Database API call failed with HTTP {(int)response.StatusCode}."
            : fallback.Trim('"');
    }
}
