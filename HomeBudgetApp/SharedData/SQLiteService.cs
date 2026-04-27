namespace SharedData
{
    using Microsoft.Data.Sqlite;

    public class DatabaseService
    {
        private readonly string _connectionString;

        public DatabaseService(string dbPath)
        {
            _connectionString = $"Data Source={dbPath}";
        }

        public void Initialize()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText =
            @"
                CREATE TABLE IF NOT EXISTS Users (
                    email TEXT PRIMARY KEY NOT NULL,
                    name TEXT NOT NULL,
                    password TEXT NOT NULL
                ); 
             ";
            command.ExecuteNonQuery();
        }


        public void CreateUser(string email, string name, string password)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
               INSERT INTO Users (email, name, password)
               VALUES ($email, $name, $password);
            ";
            command.Parameters.AddWithValue("$email", email);
            command.Parameters.AddWithValue("$name", name);
            command.Parameters.AddWithValue("$password", password);
            command.ExecuteNonQuery();
        }

        public User? GetUser(string email)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = @"
                 SELECT email, name, password
                 FROM Users
                 WHERE email = $email;
            ";

            command.Parameters.AddWithValue("$email", email);

            using var reader = command.ExecuteReader();

            if (reader.Read())
            {
                return new User
                {
                    Email = reader.GetString(0),
                    Name = reader.GetString(1),
                    Password = reader.GetString(2)
                };
            }

            return null; // user not found
        }

    }
}
