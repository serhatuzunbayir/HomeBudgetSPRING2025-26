using Microsoft.Data.Sqlite;

namespace SharedData;

public class DatabaseRepository
{
    private readonly string _connectionString;

    public DatabaseRepository(string databasePath)
    {
        _connectionString = $"Data Source={databasePath}";
        CreateTables();
    }

    private SqliteConnection GetConnection()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA foreign_keys = ON;";
        command.ExecuteNonQuery();

        return connection;
    }

    private void CreateTables()
    {
        using var connection = GetConnection();
        using var command = connection.CreateCommand();

        command.CommandText = @"
        PRAGMA foreign_keys = ON;

        CREATE TABLE IF NOT EXISTS Families (
            familyId INTEGER PRIMARY KEY AUTOINCREMENT,
            name TEXT NOT NULL
        );

        CREATE TABLE IF NOT EXISTS Users (
            userId INTEGER PRIMARY KEY AUTOINCREMENT,
            email TEXT NOT NULL UNIQUE,
            name TEXT NOT NULL,
            password TEXT NOT NULL
        );

        CREATE TABLE IF NOT EXISTS FamilyMembers (
            userId INTEGER PRIMARY KEY,
            familyId INTEGER NOT NULL,
            role TEXT NOT NULL,

            UNIQUE(familyId, userId),

            FOREIGN KEY (userId)
            REFERENCES Users(userId)
            ON DELETE CASCADE,

            FOREIGN KEY (familyId)
            REFERENCES Families(familyId)
            ON DELETE CASCADE
        );

        CREATE TABLE IF NOT EXISTS Categories (
            categoryId INTEGER PRIMARY KEY AUTOINCREMENT,
            userId INTEGER NOT NULL,
            name TEXT NOT NULL,
            budget REAL NOT NULL DEFAULT 0,

            FOREIGN KEY (userId)
            REFERENCES Users(userId)
            ON DELETE CASCADE,

            UNIQUE(userId, name),
            UNIQUE(userId, categoryId)
        );

        CREATE TABLE IF NOT EXISTS Expenses (
            expenseId INTEGER PRIMARY KEY AUTOINCREMENT,
            userId INTEGER NOT NULL,
            categoryId INTEGER NOT NULL,
            description TEXT NOT NULL,
            amount REAL NOT NULL,
            date TEXT NOT NULL,

            FOREIGN KEY (userId)
            REFERENCES Users(userId)
            ON DELETE CASCADE,

            FOREIGN KEY (userId, categoryId)
                REFERENCES Categories(userId, categoryId)
                ON DELETE CASCADE
        );

        CREATE TABLE IF NOT EXISTS FamilyCategories (
            familyCategoryId INTEGER PRIMARY KEY AUTOINCREMENT,
            familyId INTEGER NOT NULL,
            name TEXT NOT NULL,
            budget REAL NOT NULL DEFAULT 0,

            FOREIGN KEY (familyId)
            REFERENCES Families(familyId)
            ON DELETE CASCADE,

            UNIQUE(familyId, name),
            UNIQUE(familyId, familyCategoryId)
        );

        CREATE TABLE IF NOT EXISTS FamilyExpenses (
            familyExpenseId INTEGER PRIMARY KEY AUTOINCREMENT,
            familyId INTEGER NOT NULL,
            userId INTEGER NOT NULL,
            familyCategoryId INTEGER NOT NULL,
            description TEXT NOT NULL,
            amount REAL NOT NULL,
            date TEXT NOT NULL,

            FOREIGN KEY (userId)
            REFERENCES Users(userId)
            ON DELETE CASCADE,

            FOREIGN KEY (familyId)
            REFERENCES Families(familyId)
            ON DELETE CASCADE,

            FOREIGN KEY (familyId, userId)
            REFERENCES FamilyMembers(familyId, userId)
            ON DELETE CASCADE,

            FOREIGN KEY (familyId, familyCategoryId)
            REFERENCES FamilyCategories(familyId, familyCategoryId)
            ON DELETE CASCADE
        );

        ";

        command.ExecuteNonQuery();
    }

    private bool FamilyMemberExists(SqliteConnection connection, int familyId, int userId)
    {
        using var command = connection.CreateCommand();
        command.CommandText = @"
        SELECT COUNT(*)
        FROM FamilyMembers
        WHERE familyId = $familyId
          AND userId = $userId;
        ";

        command.Parameters.AddWithValue("$familyId", familyId);
        command.Parameters.AddWithValue("$userId", userId);

        return Convert.ToInt32(command.ExecuteScalar()) > 0;
    }

    private bool FamilyCategoryExists(SqliteConnection connection, int familyId, int familyCategoryId)
    {
        using var command = connection.CreateCommand();
        command.CommandText = @"
        SELECT COUNT(*)
        FROM FamilyCategories
        WHERE familyId = $familyId
          AND familyCategoryId = $familyCategoryId;
        ";

        command.Parameters.AddWithValue("$familyId", familyId);
        command.Parameters.AddWithValue("$familyCategoryId", familyCategoryId);

        return Convert.ToInt32(command.ExecuteScalar()) > 0;
    }

    private void ValidateFamilyExpenseScope(SqliteConnection connection, int familyId, int userId, int familyCategoryId)
    {
        if (!FamilyMemberExists(connection, familyId, userId))
            throw new InvalidOperationException("The user must be a member of the family before adding a family expense.");

        if (!FamilyCategoryExists(connection, familyId, familyCategoryId))
            throw new InvalidOperationException("The family category must belong to the same family as the expense.");
    }

    // ---------------- USERS ----------------

    public int AddUser(string email, string name, string password)
    {
        using var connection = GetConnection();
        using var command = connection.CreateCommand();

        command.CommandText = @"
        INSERT INTO Users (email, name, password)
        VALUES ($email, $name, $password);

        SELECT last_insert_rowid();
        ";

        command.Parameters.AddWithValue("$email", email);
        command.Parameters.AddWithValue("$name", name);
        command.Parameters.AddWithValue("$password", password);

        return Convert.ToInt32(command.ExecuteScalar());
    }

    public User? GetUser(int userId)
    {
        using var connection = GetConnection();
        using var command = connection.CreateCommand();

        command.CommandText = @"
        SELECT userId, email, name, password
        FROM Users
        WHERE userId = $userId;
        ";

        command.Parameters.AddWithValue("$userId", userId);

        using var reader = command.ExecuteReader();

        if (!reader.Read())
            return null;

        return new User
        {
            UserId = reader.GetInt32(0),
            Email = reader.GetString(1),
            Name = reader.GetString(2),
            Password = reader.GetString(3)
        };
    }

    public User? GetUserByEmail(string email)
    {
        using var connection = GetConnection();
        using var command = connection.CreateCommand();

        command.CommandText = @"
        SELECT userId, email, name, password
        FROM Users
        WHERE email = $email;
        ";

        command.Parameters.AddWithValue("$email", email);

        using var reader = command.ExecuteReader();

        if (!reader.Read())
            return null;

        return new User
        {
            UserId = reader.GetInt32(0),
            Email = reader.GetString(1),
            Name = reader.GetString(2),
            Password = reader.GetString(3)
        };
    }

    public void UpdateUser(int userId, string name, string password)
    {
        using var connection = GetConnection();
        using var command = connection.CreateCommand();

        command.CommandText = @"
        UPDATE Users
        SET name = $name,
            password = $password
        WHERE userId = $userId;
        ";

        command.Parameters.AddWithValue("$userId", userId);
        command.Parameters.AddWithValue("$name", name);
        command.Parameters.AddWithValue("$password", password);

        command.ExecuteNonQuery();
    }

    public void DeleteUser(int userId)
    {
        using var connection = GetConnection();
        using var transaction = connection.BeginTransaction();

        int? familyId = null;

        using (var command = connection.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText = @"
            SELECT familyId
            FROM FamilyMembers
            WHERE userId = $userId;
            ";

            command.Parameters.AddWithValue("$userId", userId);

            var result = command.ExecuteScalar();

            if (result != null && result != DBNull.Value)
                familyId = Convert.ToInt32(result);
        }

        using (var command = connection.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText = @"
            DELETE FROM Users
            WHERE userId = $userId;
            ";

            command.Parameters.AddWithValue("$userId", userId);
            command.ExecuteNonQuery();
        }

        if (familyId != null)
        {
            int remainingMembers;

            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = @"
                SELECT COUNT(*)
                FROM FamilyMembers
                WHERE familyId = $familyId;
                ";

                command.Parameters.AddWithValue("$familyId", familyId.Value);
                remainingMembers = Convert.ToInt32(command.ExecuteScalar());
            }

            if (remainingMembers == 0)
            {
                using var command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = @"
                DELETE FROM Families
                WHERE familyId = $familyId;
                ";

                command.Parameters.AddWithValue("$familyId", familyId.Value);
                command.ExecuteNonQuery();
            }
        }

        transaction.Commit();
    }

    // ---------------- EXPENSES ----------------


    //expense objesine id sini assign etmeyi unutmayın.
    public int AddExpense(int userId, int categoryId, string description, double amount, string date)
    {
        using var connection = GetConnection();
        using var command = connection.CreateCommand();

        command.CommandText = @"
        INSERT INTO Expenses (userId, categoryId, description, amount, date)
        VALUES ($userId, $categoryId, $description, $amount, $date);

        SELECT last_insert_rowid();
        ";

        command.Parameters.AddWithValue("$userId", userId);
        command.Parameters.AddWithValue("$categoryId", categoryId);
        command.Parameters.AddWithValue("$description", description);
        command.Parameters.AddWithValue("$amount", amount);
        command.Parameters.AddWithValue("$date", date);

        return Convert.ToInt32(command.ExecuteScalar());
    }

    public Expense? GetExpense(int expenseId, int userId)
    {
        using var connection = GetConnection();
        using var command = connection.CreateCommand();

        command.CommandText = @"
        SELECT expenseId, userId, categoryId, description, amount, date
        FROM Expenses
        WHERE expenseId = $expenseId
          AND userId = $userId;
        ";

        command.Parameters.AddWithValue("$expenseId", expenseId);
        command.Parameters.AddWithValue("$userId", userId);

        using var reader = command.ExecuteReader();

        if (!reader.Read())
            return null;

        return new Expense
        {
            ExpenseId = reader.GetInt32(0),
            UserId = reader.GetInt32(1),
            CategoryId = reader.GetInt32(2),
            Description = reader.GetString(3),
            Amount = reader.GetDouble(4),
            Date = reader.GetString(5)
        };
    }

    public List<Expense> GetAllExpensesByUser(int userId)
    {
        var expenses = new List<Expense>();

        using var connection = GetConnection();
        using var command = connection.CreateCommand();

        command.CommandText = @"
        SELECT expenseId, userId, categoryId, description, amount, date
        FROM Expenses
        WHERE userId = $userId
        ORDER BY date DESC;
        ";

        command.Parameters.AddWithValue("$userId", userId);

        using var reader = command.ExecuteReader();

        while (reader.Read())
        {
            expenses.Add(new Expense
            {
                ExpenseId = reader.GetInt32(0),
                UserId = reader.GetInt32(1),
                CategoryId = reader.GetInt32(2),
                Description = reader.GetString(3),
                Amount = reader.GetDouble(4),
                Date = reader.GetString(5)
            });
        }

        return expenses;
    }

    public void UpdateExpense(int expenseId, int userId, int categoryId, string description, double amount, string date)
    {
        using var connection = GetConnection();
        using var command = connection.CreateCommand();

        command.CommandText = @"
        UPDATE Expenses
        SET categoryId = $categoryId,
            description = $description,
            amount = $amount,
            date = $date
        WHERE expenseId = $expenseId
          AND userId = $userId;
        ";

        command.Parameters.AddWithValue("$expenseId", expenseId);
        command.Parameters.AddWithValue("$userId", userId);
        command.Parameters.AddWithValue("$categoryId", categoryId);
        command.Parameters.AddWithValue("$description", description);
        command.Parameters.AddWithValue("$amount", amount);
        command.Parameters.AddWithValue("$date", date);

        command.ExecuteNonQuery();
    }

    public void DeleteExpense(int expenseId, int userId)
    {
        using var connection = GetConnection();
        using var command = connection.CreateCommand();

        command.CommandText = @"
        DELETE FROM Expenses
        WHERE expenseId = $expenseId
          AND userId = $userId;
        ";

        command.Parameters.AddWithValue("$expenseId", expenseId);
        command.Parameters.AddWithValue("$userId", userId);

        command.ExecuteNonQuery();
    }

    // ---------------- CATEGORIES ----------------

    public int AddCategory(int userId, string name, double remainingBudget)
    {
        using var connection = GetConnection();
        using var command = connection.CreateCommand();

        command.CommandText = @"
        INSERT INTO Categories (userId, name, budget)
        VALUES ($userId, $name, $budget);

        SELECT last_insert_rowid();
        ";

        command.Parameters.AddWithValue("$userId", userId);
        command.Parameters.AddWithValue("$name", name);
        command.Parameters.AddWithValue("$budget", remainingBudget);

        return Convert.ToInt32(command.ExecuteScalar());
    }

    public Category? GetCategory(int categoryId, int userId)
    {
        using var connection = GetConnection();
        using var command = connection.CreateCommand();

        command.CommandText = @"
        SELECT categoryId, userId, name, budget
        FROM Categories
        WHERE categoryId = $categoryId
          AND userId = $userId;
        ";

        command.Parameters.AddWithValue("$categoryId", categoryId);
        command.Parameters.AddWithValue("$userId", userId);

        using var reader = command.ExecuteReader();

        if (!reader.Read())
            return null;

        return new Category
        {
            CategoryId = reader.GetInt32(0),
            UserId = reader.GetInt32(1),
            Name = reader.GetString(2),
            Budget = reader.GetDouble(3)
        };
    }

    public List<Category> GetAllCategoriesByUser(int userId)
    {
        var categories = new List<Category>();

        using var connection = GetConnection();
        using var command = connection.CreateCommand();

        command.CommandText = @"
        SELECT categoryId, userId, name, budget
        FROM Categories
        WHERE userId = $userId
        ORDER BY name ASC;
        ";

        command.Parameters.AddWithValue("$userId", userId);

        using var reader = command.ExecuteReader();

        while (reader.Read())
        {
            categories.Add(new Category
            {
                CategoryId = reader.GetInt32(0),
                UserId = reader.GetInt32(1),
                Name = reader.GetString(2),
                Budget = reader.GetDouble(3)
            });
        }

        return categories;
    }

    public void UpdateCategory(int categoryId, int userId, string name, double remainingBudget)
    {
        using var connection = GetConnection();
        using var command = connection.CreateCommand();

        command.CommandText = @"
        UPDATE Categories
        SET name = $name,
            budget = $budget
        WHERE categoryId = $categoryId
          AND userId = $userId;
        ";

        command.Parameters.AddWithValue("$categoryId", categoryId);
        command.Parameters.AddWithValue("$userId", userId);
        command.Parameters.AddWithValue("$name", name);
        command.Parameters.AddWithValue("$budget", remainingBudget);

        command.ExecuteNonQuery();
    }

    public void DeleteCategory(int categoryId, int userId)
    {
        using var connection = GetConnection();
        using var command = connection.CreateCommand();

        command.CommandText = @"
        DELETE FROM Categories
        WHERE categoryId = $categoryId
          AND userId = $userId;
        ";

        command.Parameters.AddWithValue("$categoryId", categoryId);
        command.Parameters.AddWithValue("$userId", userId);

        command.ExecuteNonQuery();
    }

    // ---------------- FAMILIES ----------------

    public int CreateFamily(int ownerUserId, string familyName, string role = "Owner")
    {
        using var connection = GetConnection();
        using var transaction = connection.BeginTransaction();

        int familyId;

        using (var command = connection.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText = @"
            INSERT INTO Families (name)
            VALUES ($name);

            SELECT last_insert_rowid();
            ";

            command.Parameters.AddWithValue("$name", familyName);

            familyId = Convert.ToInt32(command.ExecuteScalar());
        }

        using (var command = connection.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText = @"
            INSERT INTO FamilyMembers (userId, familyId, role)
            VALUES ($userId, $familyId, $role);
            ";

            command.Parameters.AddWithValue("$userId", ownerUserId);
            command.Parameters.AddWithValue("$familyId", familyId);
            command.Parameters.AddWithValue("$role", role);

            command.ExecuteNonQuery();
        }

        transaction.Commit();

        return familyId;
    }

    public string? GetFamilyName(int familyId)
    {
        using var connection = GetConnection();
        using var command = connection.CreateCommand();

        command.CommandText = @"
        SELECT name
        FROM Families
        WHERE familyId = $familyId;
        ";

        command.Parameters.AddWithValue("$familyId", familyId);

        var result = command.ExecuteScalar();

        return result?.ToString();
    }

    public Family? GetFamily(int familyId)
    {
        using var connection = GetConnection();
        using var command = connection.CreateCommand();

        command.CommandText = @"
        SELECT familyId, name
        FROM Families
        WHERE familyId = $familyId;
        ";

        command.Parameters.AddWithValue("$familyId", familyId);

        using var reader = command.ExecuteReader();

        if (!reader.Read())
            return null;

        return new Family
        {
            FamilyId = reader.GetInt32(0),
            Name = reader.GetString(1)
        };
    }

    public Family? GetFamilyByUser(int userId)
    {
        using var connection = GetConnection();
        using var command = connection.CreateCommand();

        command.CommandText = @"
        SELECT Families.familyId, Families.name
        FROM Families
        INNER JOIN FamilyMembers ON FamilyMembers.familyId = Families.familyId
        WHERE FamilyMembers.userId = $userId;
        ";

        command.Parameters.AddWithValue("$userId", userId);

        using var reader = command.ExecuteReader();

        if (!reader.Read())
            return null;

        return new Family
        {
            FamilyId = reader.GetInt32(0),
            Name = reader.GetString(1)
        };
    }

    public void UpdateFamily(int familyId, string name)
    {
        using var connection = GetConnection();
        using var command = connection.CreateCommand();

        command.CommandText = @"
        UPDATE Families
        SET name = $name
        WHERE familyId = $familyId;
        ";

        command.Parameters.AddWithValue("$familyId", familyId);
        command.Parameters.AddWithValue("$name", name);

        command.ExecuteNonQuery();
    }

    public void DeleteFamily(int familyId)
    {
        using var connection = GetConnection();
        using var command = connection.CreateCommand();

        command.CommandText = @"
        DELETE FROM Families
        WHERE familyId = $familyId;
        ";

        command.Parameters.AddWithValue("$familyId", familyId);

        command.ExecuteNonQuery();
    }

    public List<FamilyUser> GetFamilyUsers(int familyId)
    {
        var users = new List<FamilyUser>();

        using var connection = GetConnection();
        using var command = connection.CreateCommand();

        command.CommandText = @"
        SELECT Users.userId, Users.email, Users.name, FamilyMembers.role
        FROM FamilyMembers
        INNER JOIN Users ON Users.userId = FamilyMembers.userId
        WHERE FamilyMembers.familyId = $familyId
        ORDER BY Users.name ASC;
        ";

        command.Parameters.AddWithValue("$familyId", familyId);

        using var reader = command.ExecuteReader();

        while (reader.Read())
        {
            users.Add(new FamilyUser
            {
                UserId = reader.GetInt32(0),
                Email = reader.GetString(1),
                Name = reader.GetString(2),
                Role = reader.GetString(3)
            });
        }

        return users;
    }

    public void AddUserToFamily(int userId, int familyId, string role)
    {
        using var connection = GetConnection();
        using var command = connection.CreateCommand();

        command.CommandText = @"
        INSERT INTO FamilyMembers (userId, familyId, role)
        VALUES ($userId, $familyId, $role);
        ";

        command.Parameters.AddWithValue("$userId", userId);
        command.Parameters.AddWithValue("$familyId", familyId);
        command.Parameters.AddWithValue("$role", role);

        command.ExecuteNonQuery();
    }

    public void RemoveUserFromFamily(int userId)
    {
        using var connection = GetConnection();
        using var command = connection.CreateCommand();

        command.CommandText = @"
        DELETE FROM FamilyMembers
        WHERE userId = $userId;
        ";

        command.Parameters.AddWithValue("$userId", userId);

        command.ExecuteNonQuery();
    }

    public void UpdateFamilyMemberRole(int userId, int familyId, string role)
    {
        using var connection = GetConnection();
        using var command = connection.CreateCommand();

        command.CommandText = @"
        UPDATE FamilyMembers
        SET role = $role
        WHERE userId = $userId
          AND familyId = $familyId;
        ";

        command.Parameters.AddWithValue("$userId", userId);
        command.Parameters.AddWithValue("$familyId", familyId);
        command.Parameters.AddWithValue("$role", role);

        command.ExecuteNonQuery();
    }

    // ---------------- FAMILY CATEGORIES ----------------

    public int AddFamilyCategory(int familyId, string name, double budget)
    {
        using var connection = GetConnection();
        using var command = connection.CreateCommand();

        command.CommandText = @"
        INSERT INTO FamilyCategories (familyId, name, budget)
        VALUES ($familyId, $name, $budget);

        SELECT last_insert_rowid();
        ";

        command.Parameters.AddWithValue("$familyId", familyId);
        command.Parameters.AddWithValue("$name", name);
        command.Parameters.AddWithValue("$budget", budget);

        return Convert.ToInt32(command.ExecuteScalar());
    }

    public FamilyCategory? GetFamilyCategory(int familyId, int familyCategoryId)
    {
        using var connection = GetConnection();
        using var command = connection.CreateCommand();

        command.CommandText = @"
        SELECT familyCategoryId, familyId, name, budget
        FROM FamilyCategories
        WHERE familyId = $familyId
          AND familyCategoryId = $familyCategoryId;
        ";

        command.Parameters.AddWithValue("$familyId", familyId);
        command.Parameters.AddWithValue("$familyCategoryId", familyCategoryId);

        using var reader = command.ExecuteReader();

        if (!reader.Read())
            return null;

        return new FamilyCategory
        {
            FamilyCategoryId = reader.GetInt32(0),
            FamilyId = reader.GetInt32(1),
            Name = reader.GetString(2),
            Budget = reader.GetDouble(3)
        };
    }

    public List<FamilyCategory> GetAllFamilyCategories(int familyId)
    {
        var categories = new List<FamilyCategory>();

        using var connection = GetConnection();
        using var command = connection.CreateCommand();

        command.CommandText = @"
        SELECT familyCategoryId, familyId, name, budget
        FROM FamilyCategories
        WHERE familyId = $familyId
        ORDER BY name ASC;
        ";

        command.Parameters.AddWithValue("$familyId", familyId);

        using var reader = command.ExecuteReader();

        while (reader.Read())
        {
            categories.Add(new FamilyCategory
            {
                FamilyCategoryId = reader.GetInt32(0),
                FamilyId = reader.GetInt32(1),
                Name = reader.GetString(2),
                Budget = reader.GetDouble(3)
            });
        }

        return categories;
    }

    public void UpdateFamilyCategory(int familyId, int familyCategoryId, string name, double budget)
    {
        using var connection = GetConnection();
        using var command = connection.CreateCommand();

        command.CommandText = @"
        UPDATE FamilyCategories
        SET name = $name,
            budget = $budget
        WHERE familyId = $familyId
          AND familyCategoryId = $familyCategoryId;
        ";

        command.Parameters.AddWithValue("$familyId", familyId);
        command.Parameters.AddWithValue("$familyCategoryId", familyCategoryId);
        command.Parameters.AddWithValue("$name", name);
        command.Parameters.AddWithValue("$budget", budget);

        command.ExecuteNonQuery();
    }

    public void DeleteFamilyCategory(int familyId, int familyCategoryId)
    {
        using var connection = GetConnection();
        using var command = connection.CreateCommand();

        command.CommandText = @"
        DELETE FROM FamilyCategories
        WHERE familyId = $familyId
          AND familyCategoryId = $familyCategoryId;
        ";

        command.Parameters.AddWithValue("$familyId", familyId);
        command.Parameters.AddWithValue("$familyCategoryId", familyCategoryId);

        command.ExecuteNonQuery();
    }

    // ---------------- FAMILY EXPENSES ----------------

    public int AddFamilyExpense(int familyId, int userId, int familyCategoryId, string description, double amount, string date)
    {
        using var connection = GetConnection();
        ValidateFamilyExpenseScope(connection, familyId, userId, familyCategoryId);

        using var command = connection.CreateCommand();

        command.CommandText = @"
        INSERT INTO FamilyExpenses (familyId, userId, familyCategoryId, description, amount, date)
        VALUES ($familyId, $userId, $familyCategoryId, $description, $amount, $date);

        SELECT last_insert_rowid();
        ";

        command.Parameters.AddWithValue("$familyId", familyId);
        command.Parameters.AddWithValue("$userId", userId);
        command.Parameters.AddWithValue("$familyCategoryId", familyCategoryId);
        command.Parameters.AddWithValue("$description", description);
        command.Parameters.AddWithValue("$amount", amount);
        command.Parameters.AddWithValue("$date", date);

        return Convert.ToInt32(command.ExecuteScalar());
    }

    public FamilyExpense? GetFamilyExpense(int familyId, int familyExpenseId)
    {
        using var connection = GetConnection();
        using var command = connection.CreateCommand();

        command.CommandText = @"
        SELECT familyExpenseId, familyId, userId, familyCategoryId, description, amount, date
        FROM FamilyExpenses
        WHERE familyId = $familyId
          AND familyExpenseId = $familyExpenseId;
        ";

        command.Parameters.AddWithValue("$familyId", familyId);
        command.Parameters.AddWithValue("$familyExpenseId", familyExpenseId);

        using var reader = command.ExecuteReader();

        if (!reader.Read())
            return null;

        return new FamilyExpense
        {
            FamilyExpenseId = reader.GetInt32(0),
            FamilyId = reader.GetInt32(1),
            UserId = reader.GetInt32(2),
            FamilyCategoryId = reader.GetInt32(3),
            Description = reader.GetString(4),
            Amount = reader.GetDouble(5),
            Date = reader.GetString(6)
        };
    }

    public List<FamilyExpense> GetAllFamilyExpenses(int familyId)
    {
        var expenses = new List<FamilyExpense>();

        using var connection = GetConnection();
        using var command = connection.CreateCommand();

        command.CommandText = @"
        SELECT familyExpenseId, familyId, userId, familyCategoryId, description, amount, date
        FROM FamilyExpenses
        WHERE familyId = $familyId
        ORDER BY date DESC;
        ";

        command.Parameters.AddWithValue("$familyId", familyId);

        using var reader = command.ExecuteReader();

        while (reader.Read())
        {
            expenses.Add(new FamilyExpense
            {
                FamilyExpenseId = reader.GetInt32(0),
                FamilyId = reader.GetInt32(1),
                UserId = reader.GetInt32(2),
                FamilyCategoryId = reader.GetInt32(3),
                Description = reader.GetString(4),
                Amount = reader.GetDouble(5),
                Date = reader.GetString(6)
            });
        }

        return expenses;
    }

    public List<FamilyExpense> GetFamilyExpensesByUser(int familyId, int userId)
    {
        var expenses = new List<FamilyExpense>();

        using var connection = GetConnection();
        using var command = connection.CreateCommand();

        command.CommandText = @"
        SELECT familyExpenseId, familyId, userId, familyCategoryId, description, amount, date
        FROM FamilyExpenses
        WHERE familyId = $familyId
          AND userId = $userId
        ORDER BY date DESC;
        ";

        command.Parameters.AddWithValue("$familyId", familyId);
        command.Parameters.AddWithValue("$userId", userId);

        using var reader = command.ExecuteReader();

        while (reader.Read())
        {
            expenses.Add(new FamilyExpense
            {
                FamilyExpenseId = reader.GetInt32(0),
                FamilyId = reader.GetInt32(1),
                UserId = reader.GetInt32(2),
                FamilyCategoryId = reader.GetInt32(3),
                Description = reader.GetString(4),
                Amount = reader.GetDouble(5),
                Date = reader.GetString(6)
            });
        }

        return expenses;
    }

    public void UpdateFamilyExpense(int familyId, int familyExpenseId, int userId, int familyCategoryId, string description, double amount, string date)
    {
        using var connection = GetConnection();
        ValidateFamilyExpenseScope(connection, familyId, userId, familyCategoryId);

        using var command = connection.CreateCommand();

        command.CommandText = @"
        UPDATE FamilyExpenses
        SET userId = $userId,
            familyCategoryId = $familyCategoryId,
            description = $description,
            amount = $amount,
            date = $date
        WHERE familyId = $familyId
          AND familyExpenseId = $familyExpenseId;
        ";

        command.Parameters.AddWithValue("$familyId", familyId);
        command.Parameters.AddWithValue("$familyExpenseId", familyExpenseId);
        command.Parameters.AddWithValue("$userId", userId);
        command.Parameters.AddWithValue("$familyCategoryId", familyCategoryId);
        command.Parameters.AddWithValue("$description", description);
        command.Parameters.AddWithValue("$amount", amount);
        command.Parameters.AddWithValue("$date", date);

        command.ExecuteNonQuery();
    }

    public void DeleteFamilyExpense(int familyId, int familyExpenseId)
    {
        using var connection = GetConnection();
        using var command = connection.CreateCommand();

        command.CommandText = @"
        DELETE FROM FamilyExpenses
        WHERE familyId = $familyId
          AND familyExpenseId = $familyExpenseId;
        ";

        command.Parameters.AddWithValue("$familyId", familyId);
        command.Parameters.AddWithValue("$familyExpenseId", familyExpenseId);

        command.ExecuteNonQuery();
    }
}
