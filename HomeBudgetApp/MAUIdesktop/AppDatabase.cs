using SharedData;

namespace MAUIdesktop;

public static class AppDatabase
{
    private static DatabaseService? _database;

    public static DatabaseService Instance
    {
        get
        {
            if (_database is not null)
            {
                return _database;
            }

            _database = new DatabaseService(SharedDatabasePath.GetPath());
            _database.Initialize();

            return _database;
        }
    }
}
