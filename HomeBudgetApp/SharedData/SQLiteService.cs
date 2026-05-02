using Microsoft.Data.Sqlite;

namespace SharedData;

public class User
{
    public int UserId { get; set; }
    public string Email { get; set; } = "";
    public string Name { get; set; } = "";
    public string Password { get; set; } = "";
}

public class Expense
{
    public int ExpenseId { get; set; }
    public int UserId { get; set; }
    public int CategoryId { get; set; }
    public string Description { get; set; } = "";
    public double Amount { get; set; }
    public string Date { get; set; } = "";
}

public class Category
{
    public int CategoryId { get; set; }
    public int UserId { get; set; }
    public string Name { get; set; } = "";
    public double RemainingBudget { get; set; }
}

public class FamilyUser
{
    public int UserId { get; set; }
    public string Email { get; set; } = "";
    public string Name { get; set; } = "";
    public string Role { get; set; } = "";
}

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
            remainingBudget REAL NOT NULL DEFAULT 0,

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
        ";

        command.ExecuteNonQuery();
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
        INSERT INTO Categories (userId, name, remainingBudget)
        VALUES ($userId, $name, $remainingBudget);

        SELECT last_insert_rowid();
        ";

        command.Parameters.AddWithValue("$userId", userId);
        command.Parameters.AddWithValue("$name", name);
        command.Parameters.AddWithValue("$remainingBudget", remainingBudget);

        return Convert.ToInt32(command.ExecuteScalar());
    }

    public List<Category> GetAllCategoriesByUser(int userId)
    {
        var categories = new List<Category>();

        using var connection = GetConnection();
        using var command = connection.CreateCommand();

        command.CommandText = @"
        SELECT categoryId, userId, name, remainingBudget
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
                RemainingBudget = reader.GetDouble(3)
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
            remainingBudget = $remainingBudget
        WHERE categoryId = $categoryId
          AND userId = $userId;
        ";

        command.Parameters.AddWithValue("$categoryId", categoryId);
        command.Parameters.AddWithValue("$userId", userId);
        command.Parameters.AddWithValue("$name", name);
        command.Parameters.AddWithValue("$remainingBudget", remainingBudget);

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
}