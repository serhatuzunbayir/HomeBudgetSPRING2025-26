using SharedData;

namespace MAUIdesktop;

public static class AppDatabase
{
    private static DatabaseRepository? _database;

    public static DatabaseRepository Instance
    {
        get
        {
            if (_database is not null)
            {
                return _database;
            }

            var databasePath = SharedDatabasePath.GetPath();
            _database = new DatabaseRepository(databasePath);
         
            return _database;
        }
    }
}
