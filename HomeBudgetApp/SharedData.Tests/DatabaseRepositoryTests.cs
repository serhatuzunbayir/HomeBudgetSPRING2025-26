using Microsoft.Data.Sqlite;
using SharedData;
using Xunit;

namespace SharedData.Tests;

public sealed class DatabaseRepositoryTests
{
    [Fact]
    public void Constructor_MigratesExistingDatabaseToCurrentSchema()
    {
        var path = CreateDatabasePath();
        using (var connection = new SqliteConnection($"Data Source={path}"))
        {
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = @"
            CREATE TABLE Users (
                userId INTEGER PRIMARY KEY AUTOINCREMENT,
                email TEXT NOT NULL UNIQUE,
                name TEXT NOT NULL,
                password TEXT NOT NULL
            );

            CREATE TABLE Categories (
                categoryId INTEGER PRIMARY KEY AUTOINCREMENT,
                userId INTEGER NOT NULL,
                name TEXT NOT NULL,
                budget REAL NOT NULL DEFAULT 0,
                UNIQUE(userId, name),
                UNIQUE(userId, categoryId)
            );
            ";
            command.ExecuteNonQuery();
        }

        _ = new DatabaseRepository(path);

        using var migratedConnection = new SqliteConnection($"Data Source={path}");
        migratedConnection.Open();

        Assert.True(TableExists(migratedConnection, "FamilyInvitations"));
        Assert.True(TableExists(migratedConnection, "Goals"));
        Assert.True(TableExists(migratedConnection, "GoalEvents"));
        Assert.True(ColumnExists(migratedConnection, "Categories", "warningThreshold"));
    }

    [Fact]
    public void FamilyExpenseRows_ReturnAllFamilyMemberExpensesAndApplyFilters()
    {
        var db = CreateRepository();
        var ownerId = db.AddUser("owner@example.com", "Owner", "secret1");
        var memberId = db.AddUser("member@example.com", "Member", "secret1");
        var outsiderId = db.AddUser("outsider@example.com", "Outsider", "secret1");
        var familyId = db.CreateFamily(ownerId, "Yilmaz Family");
        db.AddUserToFamily(memberId, familyId, "Member");

        var ownerFoodId = db.AddCategory(ownerId, "Food", 1000);
        var memberTransportId = db.AddCategory(memberId, "Transport", 1000);
        var outsiderFoodId = db.AddCategory(outsiderId, "Food", 1000);
        db.AddExpense(ownerId, ownerFoodId, "Groceries", 90, "2026-05-01");
        db.AddExpense(memberId, memberTransportId, "Metro", 20, "2026-05-03");
        db.AddExpense(outsiderId, outsiderFoodId, "Cafe", 15, "2026-05-05");

        var allRows = db.GetFamilyExpenseRows(familyId);
        var memberRows = db.GetFamilyExpenseRows(familyId, new FamilyExpenseQuery { MemberUserId = memberId });
        var categoryRows = db.GetFamilyExpenseRows(familyId, new FamilyExpenseQuery { CategoryName = "Food" });
        var dateRows = db.GetFamilyExpenseRows(familyId, new FamilyExpenseQuery
        {
            StartDate = new DateTime(2026, 5, 2),
            EndDate = new DateTime(2026, 5, 4)
        });

        Assert.Equal(2, allRows.Count);
        Assert.DoesNotContain(allRows, row => row.UserId == outsiderId);
        Assert.Single(memberRows);
        Assert.Equal("Member", memberRows[0].MemberName);
        Assert.Single(categoryRows);
        Assert.Equal("Groceries", categoryRows[0].Description);
        Assert.Single(dateRows);
        Assert.Equal("Metro", dateRows[0].Description);
    }

    [Fact]
    public void FamilyBudgetOverview_GroupsPersonalCategoryNamesAcrossMembers()
    {
        var db = CreateRepository();
        var ownerId = db.AddUser("owner@example.com", "Owner", "secret1");
        var memberId = db.AddUser("member@example.com", "Member", "secret1");
        var familyId = db.CreateFamily(ownerId, "Yilmaz Family");
        db.AddUserToFamily(memberId, familyId, "Member");

        var ownerFoodId = db.AddCategory(ownerId, "Food", 1000);
        var memberFoodId = db.AddCategory(memberId, "food", 1000);
        var memberTransportId = db.AddCategory(memberId, "Transport", 1000);
        db.AddExpense(ownerId, ownerFoodId, "Groceries", 90, "2026-05-01");
        db.AddExpense(memberId, memberFoodId, "Dinner", 60, "2026-05-02");
        db.AddExpense(memberId, memberTransportId, "Metro", 25, "2026-05-03");

        var overview = db.GetFamilyBudgetOverview(familyId);

        Assert.Equal(2, overview.Count);
        var food = overview.Single(summary => string.Equals(summary.CategoryName, "Food", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(150, food.TotalSpent);
        Assert.Equal(2, food.ExpenseCount);
        Assert.Equal(2, food.MemberCount);
    }

    [Fact]
    public void Goals_FireReachedAndMissedEventsOnlyOnce()
    {
        var db = CreateRepository();
        var userId = db.AddUser("goal@example.com", "Goal User", "secret1");
        var categoryId = db.AddCategory(userId, "Food", 1000);

        var reachedGoalId = db.AddGoal(
            userId,
            "June savings cap",
            100,
            new DateTime(2026, 6, 30),
            new DateTime(2026, 1, 1));
        db.AddExpense(userId, categoryId, "Groceries", 40, "2026-05-01");

        var reachedEvents = db.EvaluateGoalsForUser(userId, new DateTime(2026, 7, 1));
        var duplicateReachedEvents = db.EvaluateGoalsForUser(userId, new DateTime(2026, 7, 2));

        Assert.Single(reachedEvents);
        Assert.Equal(reachedGoalId, reachedEvents[0].GoalId);
        Assert.Equal(GoalStatus.Reached, reachedEvents[0].Status);
        Assert.Empty(duplicateReachedEvents);

        var missedGoalId = db.AddGoal(
            userId,
            "Holiday spending cap",
            50,
            new DateTime(2026, 12, 31),
            new DateTime(2026, 1, 1));
        db.AddExpense(userId, categoryId, "Hotel", 80, "2026-05-02");

        var missedGoal = db.GetGoalsByUser(userId).Single(goal => goal.GoalId == missedGoalId);
        Assert.Equal(GoalStatus.Missed, missedGoal.Status);
        Assert.Contains(db.GetRecentGoalEvents(userId, 10), goalEvent =>
            goalEvent.GoalId == missedGoalId && goalEvent.Status == GoalStatus.Missed);

        db.DeleteExpense(db.GetAllExpensesByUser(userId).Single(expense => expense.Description == "Hotel").ExpenseId, userId);

        missedGoal = db.GetGoalsByUser(userId).Single(goal => goal.GoalId == missedGoalId);
        Assert.Equal(GoalStatus.Active, missedGoal.Status);
    }

    private static DatabaseRepository CreateRepository()
    {
        return new DatabaseRepository(CreateDatabasePath());
    }

    private static string CreateDatabasePath()
    {
        return Path.Combine(Path.GetTempPath(), $"homebudget-tests-{Guid.NewGuid():N}.db");
    }

    private static bool TableExists(SqliteConnection connection, string tableName)
    {
        using var command = connection.CreateCommand();
        command.CommandText = @"
        SELECT COUNT(*)
        FROM sqlite_master
        WHERE type = 'table'
          AND name = $tableName;
        ";
        command.Parameters.AddWithValue("$tableName", tableName);
        return Convert.ToInt32(command.ExecuteScalar()) > 0;
    }

    private static bool ColumnExists(SqliteConnection connection, string tableName, string columnName)
    {
        using var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA table_info({tableName});";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            if (string.Equals(reader.GetString(1), columnName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
